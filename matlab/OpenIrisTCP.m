classdef OpenIrisTCP  < handle
    %OpenIrisTCP Controls OpenIris with its text commands over TCP (MATLAB R2020b or later).
    %   See docs/user-manual/remote-control.md for the commands and the data format.
    %
    %   oi = OpenIrisTCP;
    %   oi.Connect('localhost');
    %   oi.SetSessionName('Subject01');
    %   oi.StartRecording();
    %   frame = oi.RecordEvent('StimulusOn');
    %   data = oi.GetCurrentData();
    %   oi.StopRecording();

    properties
        t
        timeout = 2   % Seconds to wait for a reply
    end

    methods
        function result = Connect(this, ip, port)
           result = 0;

            if ( ~exist('port','var') || isempty(port) )
                port = 9002;
            end

            this.t = tcpclient(ip,port);

            result = 1;
        end

        function SetSessionName(this, sessionName)
            this.Send(['SetSessionName|' sessionName]);
        end

        function StartRecording(this)
            this.Send('StartRecording');
        end

        function StopRecording(this)
            this.Send('StopRecording');
        end

        function frameNumber = RecordEvent(this, message)
            % Saves the message in the events file of the recording and returns the frame number.
            frameNumber = str2double(this.Ask(['RecordEvent|' message], false));
        end

        function data = GetCurrentData(this)
            % Data of the last processed frame.
            data = OpenIrisTCP.ParseData(this.Ask('GetData', true));
        end

        function data = WaitForData(this)
            % Data of the next processed frame (OpenIris waits up to 1 second for it).
            data = OpenIrisTCP.ParseData(this.Ask('WaitForData', true));
        end

        function Send(this, command)
            % Sends a command that has no reply.
            write(this.t, uint8(command));
        end

        function reply = Ask(this, command, isJson)
            % Sends a command and returns its reply. TCP doesn't separate messages, so keep reading
            % until the reply is complete.
            flush(this.t);
            write(this.t, uint8(command));
            reply = '';
            started = tic;
            while toc(started) < this.timeout
                if ( this.t.NumBytesAvailable > 0 )
                    reply = [reply char(read(this.t, this.t.NumBytesAvailable))]; %#ok<AGROW>
                    if ( ~isJson || OpenIrisTCP.IsCompleteJson(reply) )
                        return;
                    end
                end
                pause(0.001);
            end
            error('OpenIrisTCP:timeout', 'No reply from OpenIris to %s.', command);
        end
    end

    methods(Static = true)
        function data = ParseData(reply)
            % The full reply is in data.Left, data.Right and data.Extra. The fields LeftX, LeftY,
            % etc. are kept for scripts written for older versions.
            data = jsondecode(reply);
            data.LeftFrameNumber = data.Left.FrameNumber;
            data.LeftTime = data.Left.Seconds;
            data.LeftX = data.Left.Pupil.Center.X;
            data.LeftY = data.Left.Pupil.Center.Y;
            data.RightFrameNumber = data.Right.FrameNumber;
            data.RightTime = data.Right.Seconds;
            data.RightX = data.Right.Pupil.Center.X;
            data.RightY = data.Right.Pupil.Center.Y;
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
