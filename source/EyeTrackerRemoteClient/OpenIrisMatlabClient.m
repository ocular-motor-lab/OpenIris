classdef OpenIrisMatlabClient  < handle
    %OpenIrisMatlabClient Controls OpenIris through its .NET remote service.
    %   Uses OpenIrisRemoteClient.dll from the OpenIris folder (Windows only). For a client that
    %   works anywhere, use OpenIrisTCP or the UDP commands (see docs/user-manual/remote-control.md).
    %
    %   oi = OpenIrisMatlabClient;
    %   oi.openiris_folder = 'C:\OpenIris';
    %   oi.Connect('localhost');
    %   oi.StartRecording();

    properties
        eyeTracker
        openiris_folder = 'C:\OpenIris'   % Folder with OpenIris.exe and OpenIrisRemoteClient.dll
    end

    methods
        function result = Connect(this, ip, port)
           result = 0;

            if ( ~exist('port','var') )
                port = 9000;
            end

                NET.addAssembly(fullfile(this.openiris_folder,'OpenIrisRemoteClient.dll'));
                if ( ~exist('ip','var') )
                    ipFile = fullfile(this.openiris_folder,'IP.txt');
                    if ( exist(ipFile, 'file') )
                        ip = strtrim(fileread(ipFile));
                    else
                        ip = 'localhost';
                    end
                end

            this.eyeTracker = OpenIris.OpenIrisClient(ip, port);

            result = 1;
        end

        function result = IsRecording(this)
            status = this.eyeTracker.Status;
            result = status.Recording;
        end

        function SetSessionName(this, sessionName)
            if ( ~isempty( this.eyeTracker) )
                this.eyeTracker.ChangeSetting('SessionName',sessionName);
            end
        end

        function StartRecording(this)
            if ( ~isempty( this.eyeTracker) )
                this.eyeTracker.StartRecording();
            end
        end

        function StopRecording(this)
            if ( ~isempty( this.eyeTracker) )
                this.eyeTracker.StopRecording();
            end
        end

        function frameNumber = RecordEvent(this, message)
            % Saves the message in the events file of the recording and returns the frame number.
            % With Psychtoolbox, the message starts with the GetSecs time, to synchronize clocks.
            frameNumber = [];
            if ( ~isempty( this.eyeTracker) )
                if ( exist('GetSecs', 'file') )
                    message = [num2str(GetSecs) ' ' message];
                end
                frameNumber = this.eyeTracker.RecordEvent(message);
                frameNumber = double(frameNumber);
            end
        end

        function data = GetCurrentData(this, message)
            data =[];
            if ( ~isempty( this.eyeTracker) )
                data = this.eyeTracker.GetCurrentData();
            end
        end


        function [files]= DownloadFile(this, path)
            files = [];
            if ( ~isempty( this.eyeTracker) )
                try
                    files = this.eyeTracker.DownloadFile();
                catch ex
                    ex
                end
                files = cell(files.ToArray)';
            end
        end
    end

    methods(Static = true)

    end

end
