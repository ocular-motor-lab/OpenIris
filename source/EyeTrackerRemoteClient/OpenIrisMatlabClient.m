classdef OpenIris  < handle
    %OpenIris Summary of this class goes here
    %   Detailed explanation goes here
    
    properties
        eyeTracker
        openiris_folder = 'C:\Users\jorge\UC Berkeley\OMlab - JOM\Code\OpenIris\source\bin\x64\Debug'
    end
    
    methods
        function result = Connect(this, ip, port)
           result = 0;
           
            if ( ~exist('port','var') )
                port = 9000;
            end

                asm = NET.addAssembly(fullfile(this.openiris_folder,'EyeTrackerRemoteClient.dll'));
                if ( ~exist('ip','var') )
                    ip = fileread(fullfile(this.openiris_folder,'IP.txt'));
                end

%             this.eyeTracker = VORLab.VOG.Remote.EyeTrackerClient(ip, port);
            this.eyeTracker = OpenIris.EyeTrackerClient(ip, port);
            
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
            frameNumber = [];
            if ( ~isempty( this.eyeTracker) )
                frameNumber = this.eyeTracker.RecordEvent([num2str(GetSecs) ' ' message]);
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



