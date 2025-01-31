// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// Log files monitor and archiver

using System;
using System.Threading.Tasks;

namespace DotNet.Host.LogFilesMonitorArchiver.Processor;


/// <summary>
/// The archive request container.
/// It encapsulates the archive processor command and  async completion event.
/// </summary>
public class ArchiveCommand
{
    struct VoidResult
    {
        public static VoidResult Result { get; } = new VoidResult();
    }

    TaskCompletionSource<VoidResult> _completion = new TaskCompletionSource<VoidResult>();

    /// <summary>
    /// Defines the list of the processor commands.
    /// </summary>
    public enum ArchiveAction
    {
        /// <summary>
        /// Defines action: move to archive
        /// </summary>
        MoveToArchive,
        /// <summary>
        /// Defines action: delete from archive
        /// </summary>
        DeleteFromArchive
    }

    /// <summary>
    /// Constructs the class.
    /// </summary>
    /// <param name="action">The processor action.</param>
    public ArchiveCommand(ArchiveAction action)
    {
        Action = action;
    }

    /// <summary>
    /// Provides the instance of the archive command for Move-To-Archive action.
    /// </summary>
    public static ArchiveCommand MoveToArchive => new ArchiveCommand(ArchiveAction.MoveToArchive);

    /// <summary>
    /// Provides the instance of the archive command for Delete-From-Archive action.
    /// </summary>
    public static ArchiveCommand DeleteFromArchive => new ArchiveCommand(ArchiveAction.DeleteFromArchive);

    /// <summary>
    /// The processor action.
    /// </summary>
    public ArchiveAction Action { get; }

    /// <summary>
    /// Marks the action as a complete.
    /// </summary>
    public void MarkComplete()
    {
        _completion.TrySetResult(VoidResult.Result);
    }

    /// <summary>
    /// Marks the action as canceled.
    /// </summary>
    public void MarkCanceled()
    {
        _completion.TrySetCanceled();
    }

    /// <summary>
    /// Marks the action as a complete.
    /// </summary>
    public void MarkException(Exception ex)
    {
        Exception = ex;
        _completion.TrySetException(ex);
    }

    /// <summary>
    /// Gets the completion task.
    /// </summary>
    public Task Complete => _completion.Task;

    /// <summary>
    /// Execution exception.
    /// </summary>
    public Exception Exception { get; set; }
}