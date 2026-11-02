classdef OpenIrisMatlabClient < OpenIris.OpenIrisClient
    %OpenIrisMatlabClient Controls OpenIris through its web API. Kept for existing scripts: new code
    %   can use OpenIris.OpenIrisClient, which has the same methods and also TCP and UDP. The one
    %   difference: RecordEvent here starts the message with the GetSecs time, with Psychtoolbox.
    %
    %   oi = OpenIrisMatlabClient;
    %   oi.Connect('localhost');
    %   oi.SetSessionName('Subject01');
    %   oi.StartRecording();
    %   frame = oi.RecordEvent('StimulusOn');
    %   data = oi.GetCurrentData();      % data.Left.Pupil.Center.X, data.LeftCalibrated.HorizontalPosition...
    %   oi.StopRecording();
    %   files = oi.DownloadFile();

    methods
        function frameNumber = RecordEvent(this, message)
            % Saves the message in the events file of the recording and returns the frame number.
            % With Psychtoolbox, the message starts with the GetSecs time, to synchronize clocks.
            if ( exist('GetSecs', 'file') )
                message = [num2str(GetSecs, '%.6f') ' ' message];
            end
            frameNumber = RecordEvent@OpenIris.OpenIrisClient(this, message);
        end
    end
end
