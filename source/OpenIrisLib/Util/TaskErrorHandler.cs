// <copyright file="SerializableDictionary.cs">
//     Copyright (c) 2014-2023 Jorge Otero-Millan, Johns Hopkins University, University of California, Berkeley. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace OpenIris
{
# nullable enable

    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Runtime.ExceptionServices;

    /// <summary>
    /// Class used to handler errors when waiting for multiple tasks to finish. Importantly it will give the
    /// option to run an action when the first error occurs that may interrupt the other tasks properly not 
    /// just the one with the error. 
    /// 
    /// Typical pattern:
    /// <code>
    /// 
    ///   var errorHandler = new TaskErrorHandler(this.StopTrackingAsync);
    ///   await Task.WhenAll(
    ///      this.imageProcessor.Start().ContinueWith(errorHandler.HandleError),
    ///      this.imageGrabber.StartAsync().ContinueWith(errorHandler.HandleError));
    ///   await errorHandler.CheckForErrors();
    ///</code>
    /// </summary>
    class TaskErrorHandler
    {
        private readonly object singleActionLock;

        private readonly Action actionWhenError;
        private readonly ConcurrentBag<Exception> exceptions;

        private bool runningAction;

        /// <summary>
        /// Initializes an instance of the class TaskErrorHandler.
        /// </summary>
        /// <param name="actionWhenError">Action that should be executed if there is an error in one of the tasks.</param>
        public TaskErrorHandler(Action actionWhenError)
        {
            this.actionWhenError = actionWhenError;
            exceptions = new ConcurrentBag<Exception>();
            singleActionLock = new object();
        }

        /// <summary>
        /// Error handler to be passed to "ContinueWith" to run after a task has finished or had an error. 
        /// </summary>
        /// <param name="task">Task that preceded the call to this method.</param>
        public void HandleError(Task task)
        {
            if (task.IsFaulted is false) return;

            exceptions.Add(task.Exception);
            Trace.WriteLine("ERROR: " + task.Exception.InnerException.Message);

            // Make sure the action only gets ran once even if multiple
            // exceptions occur.
            lock (singleActionLock)
            {
                if (runningAction) return;
                runningAction = true;
            }

            actionWhenError();
        }

        /// <summary>
        /// To be called at the end of all the awaits to check if any of the tasks had actually had an error.
        /// And to wait or the stopping action to finish (if it exists)
        /// </summary>
        /// <returns>The task.</returns>
        public void CheckForErrors()
        {
            // If there was any error in any of the tasks propagate the exception.

            if (exceptions.Count > 1) throw new AggregateException(exceptions);

            // Rethrow the exception keeping the stack trace
            // https://stackoverflow.com/questions/57383/how-to-rethrow-innerexception-without-losing-stack-trace-in-c
            if (exceptions.Count == 1) ExceptionDispatchInfo.Capture(exceptions.ToArray()[0].InnerException).Throw();
        }
    }
}
