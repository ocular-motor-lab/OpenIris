classdef OpenIrisTCP  < handle
    %VOG Summary of this class goes here
    %   Detailed explanation goes here
    
    properties
        t
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
        
%         function result = IsRecording(this)
%             status = this.eyeTracker.Status;
%             result = status.Recording;
%         end
%         
%         function SetSessionName(this, sessionName)
%             if ( ~isempty( this.eyeTracker) )
%                 this.eyeTracker.ChangeSetting('SessionName',sessionName);
%             end
%         end
        
        function StartRecording(this)
            if ( ~isempty( this.t) )
                write(this.t,uint8('StartRecording'));
            end
        end
        
        function StopRecording(this)
            if ( ~isempty( this.t) )
                write(this.t,uint8('StopRecording'));
            end
        end
        
%         function frameNumber = RecordEvent(this, message)
%             frameNumber = [];
%             if ( ~isempty( this.eyeTracker) )
%                 frameNumber = this.eyeTracker.RecordEvent([num2str(GetSecs) ' ' message]);
%                 frameNumber = double(frameNumber);
%             end
%         end
        
        function data = GetCurrentData(this, message)
            if ( ~isempty( this.t) )
                write(this.t,uint8('GetData'));
                while(this.t.BytesAvailable == 0 )
                end
                datastr = char(read(this.t));
                dataarray = str2double(string(strsplit(datastr,';')'));
                data.LeftFrameNumber = dataarray(1);
                data.LeftTime = dataarray(2);
                data.LeftX = dataarray(3);
                data.LeftY = dataarray(4);
                data.RighFramenumber = dataarray(5);
                data.RightTime = dataarray(6);
                data.RightX = dataarray(7);
                data.RightY = dataarray(8);
            end
        end
%         
%         
%         function [files]= DownloadFile(this, path)
%             files = [];
%             if ( ~isempty( this.eyeTracker) )
%                 try
%                     files = this.eyeTracker.DownloadFile();
%                 catch ex
%                     ex
%                 end
%                 files = cell(files.ToArray)';
%             end
%         end
    end
    
    methods(Static = true)
        
    end
    
end



