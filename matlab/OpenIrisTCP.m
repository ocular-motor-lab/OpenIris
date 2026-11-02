classdef OpenIrisTCP < OpenIris.OpenIrisClient
    %OpenIrisTCP Controls OpenIris with its text commands over TCP (MATLAB R2020b or later). Kept for
    %   existing scripts: new code can use OpenIris.OpenIrisClient(ip, [], 'tcp'), which has the same
    %   methods. See docs/user-manual/remote-control.md for the commands and the data format.
    %
    %   oi = OpenIrisTCP;
    %   oi.Connect('localhost');
    %   oi.SetSessionName('Subject01');
    %   oi.StartRecording();
    %   frame = oi.RecordEvent('StimulusOn');
    %   data = oi.GetCurrentData();
    %   oi.StopRecording();

    methods
        function this = OpenIrisTCP()
            this.timeout = 2;
        end

        function result = Connect(this, ip, port)
            if ( ~exist('port','var') ), port = []; end
            result = Connect@OpenIris.OpenIrisClient(this, ip, port, 'tcp');
        end

        function data = GetCurrentData(this, ~)
            % Data of the last processed frame.
            data = OpenIrisTCP.AddOldFields(GetCurrentData@OpenIris.OpenIrisClient(this));
        end

        function data = WaitForData(this)
            % Data of the next processed frame (OpenIris waits up to 1 second for it).
            data = OpenIrisTCP.AddOldFields(this.WaitForNewData());
        end

        function Send(this, command)
            % Sends a command that has no reply.
            this.CommandSend(command);
        end

        function reply = Ask(this, command, isJson)
            % Sends a command and returns its reply.
            reply = this.CommandAsk(command, isJson);
        end
    end

    methods (Static, Access = private)
        function data = AddOldFields(data)
            % The full reply is in data.Left, data.Right and data.Extra. The fields LeftX, LeftY,
            % etc. are kept for scripts written for older versions.
            data.LeftFrameNumber = data.Left.FrameNumber;
            data.LeftTime = data.Left.Seconds;
            data.LeftX = data.Left.Pupil.Center.X;
            data.LeftY = data.Left.Pupil.Center.Y;
            data.RightFrameNumber = data.Right.FrameNumber;
            data.RightTime = data.Right.Seconds;
            data.RightX = data.Right.Pupil.Center.X;
            data.RightY = data.Right.Pupil.Center.Y;
        end
    end
end
