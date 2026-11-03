classdef OpenIrisClient < handle
    %OpenIrisClient Controls OpenIris from MATLAB, over HTTP, TCP or UDP (any OS, no DLL).
    %   Replaces the old .NET client (OpenIrisRemoteClient.dll), with the same name and methods:
    %   code that did OpenIris.OpenIrisClient(ip, port) keeps working with this folder (matlab) on
    %   the path, and without loading the DLL. See docs/user-manual/remote-control.md.
    %
    %   oi = OpenIris.OpenIrisClient('localhost');          % HTTP, the web API (port 9001)
    %   oi = OpenIris.OpenIrisClient('localhost', [], 'udp'); % text commands over UDP (port 9003)
    %   oi.ChangeSetting('SessionName', 'Subject01');
    %   oi.StartRecording();
    %   frame = oi.RecordEvent('StimulusOn');
    %   data = oi.GetCurrentData();      % data.Left.Pupil.Center.X, data.LeftCalibrated.HorizontalPosition...
    %   oi.StopRecording();
    %   if ( oi.Status.Recording ), ... end
    %   files = oi.DownloadFile();
    %
    %   With 'tcp' or 'udp', recording, events, data, the session name and calibration messages go
    %   with the text commands. Everything else (Status, other settings, DownloadFile...) has no text
    %   command and goes over HTTP to the same computer.

    properties
        openiris_folder = 'C:\OpenIris'   % Folder with IP.txt (optional) and where DownloadedData goes
        timeout = 5                       % Seconds to wait for a reply
    end

    properties (SetAccess = protected)
        Protocol = ''   % 'http', 'tcp' or 'udp'
        Host = ''       % Address of the OpenIris computer
        Port = []       % Port of the protocol
    end

    properties (Dependent)
        Status          % Tracking, Recording, Calibrating, SessionName, FrameRate... (HTTP)
    end

    properties (Access = protected)
        url = ''        % Address of the web API
        connection = [] % tcpclient or udpport, for the text commands
    end

    methods
        function this = OpenIrisClient(ip, port, protocol)
            % OpenIrisClient(ip, port, protocol). protocol: 'http' (default), 'tcp' or 'udp'. Without
            % ip, the one in openiris_folder\IP.txt, or localhost. Without port (or 9000, the port of
            % the old client), the protocol's default: 9001, 9002 or 9003. Without arguments it
            % doesn't connect: call Connect.
            if ( nargin == 0 )
                return;
            end
            if ( ~exist('port','var') ), port = []; end
            if ( ~exist('protocol','var') ), protocol = 'http'; end
            this.Connect(ip, port, protocol);
        end

        function result = Connect(this, ip, port, protocol)
            % Connect(ip, port, protocol), as the constructor.
            if ( ~exist('ip','var') || isempty(ip) )
                ipFile = fullfile(this.openiris_folder, 'IP.txt');
                if ( exist(ipFile, 'file') )
                    ip = strtrim(fileread(ipFile));
                else
                    ip = 'localhost';
                end
            end
            if ( ~exist('protocol','var') || isempty(protocol) )
                protocol = 'http';
            end
            protocol = lower(char(protocol));

            % The ports are numbered from OpenIris's ServiceListeningPort (9000): web API + 1, TCP
            % + 2, UDP + 3.
            offsets = struct('http', 1, 'tcp', 2, 'udp', 3);
            if ( ~isfield(offsets, protocol) )
                error('OpenIris:Protocol', 'The protocol must be http, tcp or udp, not %s.', protocol);
            end
            if ( ~exist('port','var') || isempty(port) || port == 9000 )
                port = 9000 + offsets.(protocol);
            end

            this.Disconnect();
            this.Protocol = protocol;
            this.Host = char(ip);
            this.Port = port;
            this.url = sprintf('http://%s:%d/api', this.Host, port - offsets.(protocol) + 1);

            switch ( protocol )
                case 'http'
                    this.HttpGet('/status');   % Fails here if OpenIris is not there.
                case 'tcp'
                    this.connection = tcpclient(this.Host, port, 'ConnectTimeout', this.timeout);
                case 'udp'
                    this.connection = udpport;
            end
            result = 1;
        end

        function Disconnect(this)
            this.connection = [];
        end

        function delete(this)
            this.Disconnect();
        end

        function status = get.Status(this)
            status = [];
            if ( isempty(this.url) ), return; end   % Not connected (for example, displaying the object).
            status = this.HttpGet('/status', 'Status');
        end

        function status = GetStatus(this)
            status = this.Status;
        end

        function result = IsRecording(this)
            result = this.Status.Recording;
        end

        function SetSessionName(this, sessionName)
            this.ChangeSetting('SessionName', sessionName);
        end

        function ChangeSetting(this, name, value)
            % ChangeSetting('SessionName','S01'), ChangeSetting('Tracking.DarkThresholdLeftEye', 60)...
            % The names are in the Settings section of the web page (http://localhost:9001).
            if ( this.UsesCommands() && strcmp(name, 'SessionName') )
                this.CommandSend(['SetSessionName|' char(value)]);
            else
                this.HttpSend('put', ['/settings/' name], jsonencode(value), 'application/json', 'ChangeSetting');
            end
        end

        function StartRecording(this)
            if ( this.UsesCommands() )
                this.CommandSend('StartRecording');
            else
                this.HttpSend('post', '/recording/start');
            end
        end

        function StopRecording(this)
            if ( this.UsesCommands() )
                this.CommandSend('StopRecording');
            else
                this.HttpSend('post', '/recording/stop');
            end
        end

        function ResetReference(this)
            this.HttpSend('post', '/reference/reset', [], [], 'ResetReference');
        end

        function frameNumber = RecordEvent(this, message)
            % Saves the message in the events file of the recording and returns the frame number.
            if ( this.UsesCommands() )
                frameNumber = str2double(this.CommandAsk(['RecordEvent|' char(message)], false));
            else
                reply = this.HttpSend('post', '/events', message, 'text/plain');
                frameNumber = double(reply.FrameNumber);
            end
        end

        function data = GetCurrentData(this, ~)
            % Data of the last frame. With every protocol: data.FrameNumber, data.Seconds, and
            % data.Left and data.Right with Pupil (Center.X, Center.Y, Size...). Over HTTP, also
            % the rest of each eye's data (CornealReflections, TorsionAngle...), data.LeftCalibrated
            % and data.RightCalibrated (HorizontalPosition, VerticalPosition, TorsionalPosition in
            % degrees), Head and Extra; with the text commands, the reflections are Left.CRs. NaN
            % when not found.
            if ( this.UsesCommands() )
                data = OpenIris.OpenIrisClient.CommandData(this.CommandAsk('GetData', true));
            else
                data = this.HttpGet('/data');
            end
        end

        function data = WaitForNewData(this)
            % Waits for the next frame (up to 1 s) and returns its data, as GetCurrentData.
            if ( this.UsesCommands() )
                data = OpenIris.OpenIrisClient.CommandData(this.CommandAsk('WaitForData', true));
            else
                data = this.HttpGet('/data/next');
            end
        end

        function files = DownloadFile(this, ~, ~)
            % Downloads the data, calibration, events and video frames files of the last recording into
            % openiris_folder\DownloadedData. Waits for the recording to stop. Returns the paths, in a
            % cell array. Always over HTTP.
            while ( this.IsRecording() )
                pause(0.5);
            end

            folder = fullfile(this.openiris_folder, 'DownloadedData');
            if ( ~exist(folder, 'dir') )
                mkdir(folder);
            end

            last = this.HttpGet('/files/last', 'DownloadFile');
            names = last.Files;
            if ( ischar(names) )
                names = {names};
            elseif ( ~iscell(names) )
                names = {};   % No files.
            end
            files = cell(numel(names), 1);
            for i = 1:numel(names)
                files{i} = websave(fullfile(folder, names{i}), [this.url '/files/' urlencode(names{i})], ...
                    weboptions('Timeout', 60));
            end
        end

        function reply = CalibrationMessage(this, message)
            % Sends a message to the running calibration (N-point in its Remote mode: FixStart:x,y, FixStop,
            % FixAbort, Complete) and returns the reply ('ack' or an error).
            if ( this.UsesCommands() )
                reply = this.CommandAsk(['Calibration|' char(message)], false);
            else
                reply = this.HttpSend('post', '/calibration/message', message, 'text/plain');
                reply = reply.Reply;
            end
        end

        function StartCalibration(this)
            this.HttpSend('post', '/calibration/start', [], [], 'StartCalibration');
        end

        function CancelCalibration(this)
            this.HttpSend('post', '/calibration/cancel', [], [], 'CancelCalibration');
        end
    end

    methods (Access = protected)
        function result = UsesCommands(this)
            result = ~isempty(this.connection);
        end

        function CommandSend(this, command)
            % Sends a text command that has no reply. Commands end with a newline, so OpenIris can tell
            % them apart when several arrive together over TCP (OpenIris 0.2 or later).
            this.Write(command);
        end

        function reply = CommandAsk(this, command, isJson)
            % Sends a text command and returns its reply. TCP doesn't separate messages, so it reads
            % until the reply is complete.
            flush(this.connection);
            this.Write(command);
            reply = '';
            started = tic;
            while toc(started) < this.timeout
                if ( this.connection.NumBytesAvailable > 0 )
                    reply = [reply char(read(this.connection, this.connection.NumBytesAvailable, 'uint8'))]; %#ok<AGROW>
                    if ( ~isJson || OpenIris.OpenIrisClient.IsCompleteJson(reply) )
                        return;
                    end
                end
                pause(0.001);
            end
            error('OpenIris:Timeout', 'No reply from OpenIris to %s.', command);
        end

        function Write(this, command)
            bytes = uint8([char(command) newline]);
            if ( strcmp(this.Protocol, 'udp') )
                write(this.connection, bytes, 'uint8', this.Host, this.Port);
            else
                write(this.connection, bytes);
            end
        end

        function reply = HttpGet(this, path, method)
            if ( ~exist('method','var') ), method = ''; end
            reply = this.HttpSend('GET', path, [], [], method);
        end

        function reply = HttpSend(this, method, path, body, mediaType, clientMethod)
            % HTTP request with matlab.net.http, which, unlike webwrite, also gives the reply when
            % OpenIris answers with an error ({"error": "..."}, for example recording while not tracking).
            % clientMethod: name of the method that needs HTTP, for the error when it isn't reachable
            % while using the text commands.
            import matlab.net.http.*
            if ( isempty(this.url) )
                error('OpenIris:NotConnected', 'Call Connect first.');
            end

            request = RequestMessage(upper(method));
            if ( (~exist('body','var') || isempty(body)) && ~strcmpi(method, 'GET') )
                % Commands (start recording...) have no data, but POST expects a body.
                body = ' ';
                mediaType = 'text/plain';
            end
            if ( exist('body','var') && ~isempty(body) )
                request.Header = field.ContentTypeField([mediaType '; charset=utf-8']);
                request.Body = MessageBody();
                request.Body.Payload = unicode2native(char(body), 'UTF-8');
            end
            options = HTTPOptions('ConnectTimeout', this.timeout, 'ResponseTimeout', this.timeout, ...
                'ConvertResponse', false, 'SavePayload', true);
            try
                response = request.send(matlab.net.URI([this.url path]), options);
            catch ex
                if ( this.UsesCommands() && exist('clientMethod','var') && ~isempty(clientMethod) )
                    error('OpenIris:NoHttp', '%s needs the web API (HTTP, %s), which isn''t reachable: %s', ...
                        clientMethod, this.url, ex.message);
                end
                rethrow(ex);
            end

            text = '';
            if ( ~isempty(response.Body) && ~isempty(response.Body.Payload) )
                text = native2unicode(response.Body.Payload(:)', 'UTF-8');
            end
            if ( response.StatusCode ~= StatusCode.OK )
                message = char(response.StatusLine.ReasonPhrase);
                try
                    message = jsondecode(text).error;
                catch
                end
                error('OpenIris:Error', 'OpenIris: %s', message);
            end
            reply = OpenIris.OpenIrisClient.Decode(text);
        end
    end

    methods (Static, Access = protected)
        function value = Decode(text)
            if ( isempty(text) )
                value = [];
                return;
            end
            value = OpenIris.OpenIrisClient.NullToNaN(jsondecode(text));
        end

        function value = NullToNaN(value)
            % OpenIris sends NaN (eye not found) as null, which jsondecode turns into [] in structs.
            if ( isstruct(value) )
                for i = 1:numel(value)
                    names = fieldnames(value);
                    for f = 1:numel(names)
                        field = value(i).(names{f});
                        if ( isempty(field) && isnumeric(field) )
                            value(i).(names{f}) = NaN;
                        else
                            value(i).(names{f}) = OpenIris.OpenIrisClient.NullToNaN(field);
                        end
                    end
                end
            elseif ( iscell(value) )
                value = cellfun(@OpenIris.OpenIrisClient.NullToNaN, value, 'UniformOutput', false);
            end
        end

        function data = CommandData(text)
            % The data of the text commands, with FrameNumber and Seconds at the top, as over HTTP.
            data = OpenIris.OpenIrisClient.Decode(text);
            data.FrameNumber = data.Left.FrameNumber;
            data.Seconds = data.Left.Seconds;
            if ( isnan(data.FrameNumber) )
                data.FrameNumber = data.Right.FrameNumber;
                data.Seconds = data.Right.Seconds;
            end
        end

        function complete = IsCompleteJson(text)
            complete = false;
            if ( isempty(text) || text(end) ~= '}' )
                return;
            end
            try
                jsondecode(text);
                complete = true;
            catch
            end
        end
    end
end
