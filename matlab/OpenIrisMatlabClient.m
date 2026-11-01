classdef OpenIrisMatlabClient  < handle
    %OpenIrisMatlabClient Controls OpenIris through its web API (MATLAB R2016b or later, any OS).
    %   Needs no DLL: it talks HTTP to OpenIris (port 9001). The methods are the same as in the old
    %   client, which used OpenIrisRemoteClient.dll. See docs/user-manual/remote-control.md.
    %
    %   oi = OpenIrisMatlabClient;
    %   oi.Connect('localhost');
    %   oi.SetSessionName('Subject01');
    %   oi.StartRecording();
    %   frame = oi.RecordEvent('StimulusOn');
    %   data = oi.GetCurrentData();      % data.Left.Pupil.Center.X, data.LeftCalibrated.HorizontalPosition...
    %   oi.StopRecording();
    %   files = oi.DownloadFile();

    properties
        url = ''                          % Address of the OpenIris web API, set by Connect
        openiris_folder = 'C:\OpenIris'   % Folder with IP.txt (optional) and where DownloadedData goes
        timeout = 5                       % Seconds to wait for a reply
    end

    methods
        function result = Connect(this, ip, port)
            % Connect(ip, port). Without ip, uses the one in openiris_folder\IP.txt, or localhost.
            % The port is OpenIris's ServiceListeningPort + 1 (9001). 9000, the port of the old
            % client, also works.
            result = 0;

            if ( ~exist('port','var') || isempty(port) )
                port = 9001;
            elseif ( port == 9000 )
                port = 9001;
            end

            if ( ~exist('ip','var') || isempty(ip) )
                ipFile = fullfile(this.openiris_folder,'IP.txt');
                if ( exist(ipFile, 'file') )
                    ip = strtrim(fileread(ipFile));
                else
                    ip = 'localhost';
                end
            end

            this.url = sprintf('http://%s:%d/api', ip, port);
            this.Get('/status');   % Fails here if OpenIris is not there.

            result = 1;
        end

        function status = GetStatus(this)
            % Tracking, Recording, Calibrating, SessionName, FrameRate...
            status = this.Get('/status');
        end

        function result = IsRecording(this)
            status = this.Get('/status');
            result = status.Recording;
        end

        function SetSessionName(this, sessionName)
            this.ChangeSetting('SessionName', sessionName);
        end

        function ChangeSetting(this, name, value)
            % ChangeSetting('SessionName','S01'), ChangeSetting('Tracking.DarkThresholdLeftEye', 60)...
            % The names are in the Settings section of the web page (http://localhost:9001).
            this.Send('put', ['/settings/' name], jsonencode(value), 'application/json');
        end

        function StartRecording(this)
            this.Send('post', '/recording/start');
        end

        function StopRecording(this)
            this.Send('post', '/recording/stop');
        end

        function ResetReference(this)
            this.Send('post', '/reference/reset');
        end

        function frameNumber = RecordEvent(this, message)
            % Saves the message in the events file of the recording and returns the frame number.
            % With Psychtoolbox, the message starts with the GetSecs time, to synchronize clocks.
            if ( exist('GetSecs', 'file') )
                message = [num2str(GetSecs, '%.6f') ' ' message];
            end
            reply = this.Send('post', '/events', message, 'text/plain');
            frameNumber = double(reply.FrameNumber);
        end

        function data = GetCurrentData(this, ~)
            % Data of the last frame: data.Left and data.Right (raw: Pupil, CornealReflections,
            % TorsionAngle...), data.LeftCalibrated and data.RightCalibrated (HorizontalPosition,
            % VerticalPosition, TorsionalPosition in degrees), Head, Extra. NaN when not found.
            data = this.Get('/data');
        end

        function data = WaitForNewData(this)
            % Waits for the next frame (up to 1 s) and returns its data, as GetCurrentData.
            data = this.Get('/data/next');
        end

        function files = DownloadFile(this, ~)
            % Downloads the data, calibration, events and video frames files of the last recording into
            % openiris_folder\DownloadedData. Waits for the recording to stop. Returns the paths.
            while ( this.IsRecording() )
                pause(0.5);
            end

            folder = fullfile(this.openiris_folder, 'DownloadedData');
            if ( ~exist(folder, 'dir') )
                mkdir(folder);
            end

            last = this.Get('/files/last');
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
            % Sends a message to the running calibration (Remote Linear: FixStart:x,y, FixStop,
            % FixAbort, Complete) and returns the reply ('ack' or an error).
            reply = this.Send('post', '/calibration/message', message, 'text/plain');
            reply = reply.Reply;
        end

        function StartCalibration(this)
            this.Send('post', '/calibration/start');
        end

        function CancelCalibration(this)
            this.Send('post', '/calibration/cancel');
        end
    end

    methods (Access = private)
        function reply = Get(this, path)
            reply = this.Send('GET', path);
        end

        function reply = Send(this, method, path, body, mediaType)
            % HTTP request with matlab.net.http, which, unlike webwrite, also gives the reply when
            % OpenIris answers with an error ({"error": "..."}, for example recording while not tracking).
            import matlab.net.http.*
            if ( isempty(this.url) )
                error('OpenIris:NotConnected', 'Call Connect first.');
            end

            request = RequestMessage(upper(method));
            if ( ~exist('body','var') && ~strcmpi(method, 'GET') )
                % Commands (start recording...) have no data, but POST expects a body.
                body = ' ';
                mediaType = 'text/plain';
            end
            if ( exist('body','var') )
                request.Header = field.ContentTypeField([mediaType '; charset=utf-8']);
                request.Body = MessageBody();
                request.Body.Payload = unicode2native(char(body), 'UTF-8');
            end
            options = HTTPOptions('ConnectTimeout', this.timeout, 'ResponseTimeout', this.timeout, ...
                'ConvertResponse', false, 'SavePayload', true);
            response = request.send(matlab.net.URI([this.url path]), options);

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
            reply = OpenIrisMatlabClient.Decode(text);
        end
    end

    methods (Static, Access = private)
        function value = Decode(text)
            if ( isempty(text) )
                value = [];
                return;
            end
            value = OpenIrisMatlabClient.NullToNaN(jsondecode(text));
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
                            value(i).(names{f}) = OpenIrisMatlabClient.NullToNaN(field);
                        end
                    end
                end
            elseif ( iscell(value) )
                value = cellfun(@OpenIrisMatlabClient.NullToNaN, value, 'UniformOutput', false);
            end
        end
    end
end
