﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSConstants = Microsoft.VisualStudio.VSConstants;

namespace Microsoft.VisualStudioTools.Project.Automation
{
    /// <summary>
    /// Represents an automation object for a file in a project
    /// </summary>
    [ComVisible(true)]
    public class OAFileItem : OAProjectItem
    {
        #region ctors
        internal OAFileItem(OAProject project, FileNode node)
            : base(project, node)
        {
        }

        #endregion

        private new FileNode Node => (FileNode)base.Node;

        public override string Name
        {
            get
            {
                return this.Node.FileName;
            }
            set
            {
                this.Node.ProjectMgr.Site.GetUIThread().Invoke(() => base.Name = value);
            }
        }

        #region overridden methods
        /// <summary>
        /// Returns the dirty state of the document.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown if the project is closed or it the service provider attached to the project is invalid.</exception>
        /// <exception cref="ComException">Is thrown if the dirty state cannot be retrived.</exception>
        public override bool IsDirty
        {
            get
            {
                CheckProjectIsValid();

                var isDirty = false;

                using (var scope = new AutomationScope(this.Node.ProjectMgr.Site))
                {
                    this.Node.ProjectMgr.Site.GetUIThread().Invoke(() =>
                    {
                        var manager = this.Node.GetDocumentManager();
                        Utilities.CheckNotNull(manager);

                        isDirty = manager.IsDirty;
                    });
                }
                return isDirty;
            }
        }

        /// <summary>
        /// Gets the Document associated with the item, if one exists.
        /// </summary>
        public override EnvDTE.Document Document
        {
            get
            {
                CheckProjectIsValid();

                EnvDTE.Document document = null;

                using (var scope = new AutomationScope(this.Node.ProjectMgr.Site))
                {
                    this.Node.ProjectMgr.Site.GetUIThread().Invoke(() =>
                    {


                        VsShellUtilities.IsDocumentOpen(this.Node.ProjectMgr.Site, this.Node.Url, VSConstants.LOGVIEWID_Any, out var hier, out var itemid, out var windowFrame);

                        if (windowFrame != null)
                        {
                            ErrorHandler.ThrowOnFailure(windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocCookie, out var var));
                            ErrorHandler.ThrowOnFailure(scope.Extensibility.GetDocumentFromDocCookie((int)var, out var documentAsObject));
                            Utilities.CheckNotNull(documentAsObject);

                            document = (Document)documentAsObject;
                        }
                    });
                }

                return document;
            }
        }

        /// <summary>
        /// Opens the file item in the specified view.
        /// </summary>
        /// <param name="ViewKind">Specifies the view kind in which to open the item (file)</param>
        /// <returns>Window object</returns>
        public override EnvDTE.Window Open(string viewKind)
        {
            CheckProjectIsValid();

            IVsWindowFrame windowFrame = null;
            var docData = IntPtr.Zero;

            using (var scope = new AutomationScope(this.Node.ProjectMgr.Site))
            {
                this.Node.ProjectMgr.Site.GetUIThread().Invoke(() =>
                {
                    try
                    {
                        // Validate input params
                        var logicalViewGuid = VSConstants.LOGVIEWID_Primary;
                        try
                        {
                            if (!(string.IsNullOrEmpty(viewKind)))
                            {
                                logicalViewGuid = new Guid(viewKind);
                            }
                        }
                        catch (FormatException)
                        {
                            // Not a valid guid
                            throw new ArgumentException(SR.GetString(SR.ParameterMustBeAValidGuid), nameof(viewKind));
                        }
                        var rdt = this.Node.ProjectMgr.Site.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
                        if (rdt == null)
                        {
                            throw new InvalidOperationException("Could not get running document table from the services exposed by this project");
                        }

                        ErrorHandler.ThrowOnFailure(rdt.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, this.Node.Url, out var ivsHierarchy, out var itemid, out docData, out var docCookie));

                        // Open the file using the IVsProject interface
                        // We get the outer hierarchy so that projects can customize opening.
                        var project = this.Node.ProjectMgr.GetOuterInterface<IVsProject>();
                        ErrorHandler.ThrowOnFailure(project.OpenItem(this.Node.ID, ref logicalViewGuid, docData, out windowFrame));
                    }
                    finally
                    {
                        if (docData != IntPtr.Zero)
                        {
                            Marshal.Release(docData);
                        }
                    }
                });
            }

            // Get the automation object and return it
            return ((windowFrame != null) ? VsShellUtilities.GetWindowObject(windowFrame) : null);
        }

        /// <summary>
        /// Saves the project item.
        /// </summary>
        /// <param name="fileName">The name with which to save the project or project item.</param>
        /// <exception cref="InvalidOperationException">Is thrown if the save operation failes.</exception>
        /// <exception cref="ArgumentNullException">Is thrown if fileName is null.</exception>
        public override void Save(string fileName)
        {
            this.Node.ProjectMgr.Site.GetUIThread().Invoke(() =>
            {
                this.DoSave(false, fileName);
            });
        }

        /// <summary>
        /// Saves the project item.
        /// </summary>
        /// <param name="fileName">The file name with which to save the solution, project, or project item. If the file exists, it is overwritten</param>
        /// <returns>true if the rename was successful. False if Save as failes</returns>
        public override bool SaveAs(string fileName)
        {
            try
            {
                this.Node.ProjectMgr.Site.GetUIThread().Invoke(() =>
                {
                    this.DoSave(true, fileName);
                });
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (COMException)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets a value indicating whether the project item is open in a particular view type. 
        /// </summary>
        /// <param name="viewKind">A Constants.vsViewKind* indicating the type of view to check./param>
        /// <returns>A Boolean value indicating true if the project is open in the given view type; false if not. </returns>
        public override bool get_IsOpen(string viewKind)
        {
            CheckProjectIsValid();

            // Validate input params
            var logicalViewGuid = VSConstants.LOGVIEWID_Primary;
            try
            {
                if (!(string.IsNullOrEmpty(viewKind)))
                {
                    logicalViewGuid = new Guid(viewKind);
                }
            }
            catch (FormatException)
            {
                // Not a valid guid
                throw new ArgumentException(SR.GetString(SR.ParameterMustBeAValidGuid), nameof(viewKind));
            }

            var isOpen = false;

            using (var scope = new AutomationScope(this.Node.ProjectMgr.Site))
            {
                this.Node.ProjectMgr.Site.GetUIThread().Invoke(() =>
                {
                    isOpen = VsShellUtilities.IsDocumentOpen(this.Node.ProjectMgr.Site, this.Node.Url, logicalViewGuid, out var hier, out var itemid, out var windowFrame);
                });
            }

            return isOpen;
        }

        /// <summary>
        /// Gets the ProjectItems for the object.
        /// </summary>
        public override ProjectItems ProjectItems
        {
            get
            {
                return this.Node.ProjectMgr.Site.GetUIThread().Invoke<ProjectItems>(() =>
                {
                    if (this.Project.ProjectNode.CanFileNodesHaveChilds)
                    {
                        return new OAProjectItems(this.Project, this.Node);
                    }
                    else
                    {
                        return base.ProjectItems;
                    }
                });
            }
        }

        #endregion

        #region helpers
        /// <summary>
        /// Saves or Save As the  file
        /// </summary>
        /// <param name="isCalledFromSaveAs">Flag determining which Save method called , the SaveAs or the Save.</param>
        /// <param name="fileName">The name of the project file.</param>        
        private void DoSave(bool isCalledFromSaveAs, string fileName)
        {
            Utilities.ArgumentNotNull("fileName", fileName);

            CheckProjectIsValid();

            using (var scope = new AutomationScope(this.Node.ProjectMgr.Site))
            {
                this.Node.ProjectMgr.Site.GetUIThread().Invoke(() =>
                {
                    var docData = IntPtr.Zero;

                    try
                    {
                        var rdt = this.Node.ProjectMgr.Site.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
                        if (rdt == null)
                        {
                            throw new InvalidOperationException("Could not get running document table from the services exposed by this project");
                        }
                        int canceled;
                        var url = this.Node.Url;

                        ErrorHandler.ThrowOnFailure(rdt.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, url, out var ivsHierarchy, out var itemid, out docData, out var docCookie));

                        // If an empty file name is passed in for Save then make the file name the project name.
                        if (!isCalledFromSaveAs && fileName.Length == 0)
                        {
                            ErrorHandler.ThrowOnFailure(this.Node.ProjectMgr.SaveItem(VSSAVEFLAGS.VSSAVE_SilentSave, url, this.Node.ID, docData, out canceled));
                        }
                        else
                        {
                            Utilities.ValidateFileName(this.Node.ProjectMgr.Site, fileName);

                            // Compute the fullpath from the directory of the existing Url.
                            var fullPath = CommonUtils.GetAbsoluteFilePath(Path.GetDirectoryName(url), fileName);

                            if (!isCalledFromSaveAs)
                            {
                                if (!CommonUtils.IsSamePath(this.Node.Url, fullPath))
                                {
                                    throw new InvalidOperationException();
                                }

                                ErrorHandler.ThrowOnFailure(this.Node.ProjectMgr.SaveItem(VSSAVEFLAGS.VSSAVE_SilentSave, fullPath, this.Node.ID, docData, out canceled));
                            }
                            else
                            {
                                ErrorHandler.ThrowOnFailure(this.Node.ProjectMgr.SaveItem(VSSAVEFLAGS.VSSAVE_SilentSave, fullPath, this.Node.ID, docData, out canceled));
                            }
                        }

                        if (canceled == 1)
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    catch (COMException e)
                    {
                        throw new InvalidOperationException(e.Message);
                    }
                    finally
                    {
                        if (docData != IntPtr.Zero)
                        {
                            Marshal.Release(docData);
                        }
                    }
                });
            }
        }
        #endregion
    }
}
