// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudioTools
{
    internal class ProjectEventArgs : EventArgs
    {
        public IVsProject Project { get; }

        public ProjectEventArgs(IVsProject project)
        {
            this.Project = project;
        }
    }

    internal class SolutionEventsListener : IVsSolutionEvents3, IVsSolutionEvents4, IVsUpdateSolutionEvents2, IVsUpdateSolutionEvents3, IDisposable
    {
        private readonly IVsSolution _solution;
        private readonly IVsSolutionBuildManager3 _buildManager;
        private uint _cookie1 = VSConstants.VSCOOKIE_NIL;
        private uint _cookie2 = VSConstants.VSCOOKIE_NIL;
        private uint _cookie3 = VSConstants.VSCOOKIE_NIL;

        public event EventHandler SolutionOpened;
        public event EventHandler SolutionClosed;
        public event EventHandler<ProjectEventArgs> ProjectLoaded;
        public event EventHandler<ProjectEventArgs> ProjectUnloading;
        public event EventHandler<ProjectEventArgs> ProjectClosing;
        public event EventHandler<ProjectEventArgs> ProjectRenamed;
        public event EventHandler BuildCompleted;
        public event EventHandler BuildStarted;
        public event EventHandler ActiveSolutionConfigurationChanged;

        public SolutionEventsListener(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this._solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            if (this._solution == null)
            {
                throw new InvalidOperationException("Cannot get solution service");
            }
            this._buildManager = serviceProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager3;
        }

        public SolutionEventsListener(IVsSolution service, IVsSolutionBuildManager3 buildManager = null)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }
            this._solution = service;
            this._buildManager = buildManager;
        }

        public void StartListeningForChanges()
        {
            ErrorHandler.ThrowOnFailure(this._solution.AdviseSolutionEvents(this, out this._cookie1));
            if (this._buildManager != null)
            {
                var bm2 = this._buildManager as IVsSolutionBuildManager2;
                if (bm2 != null)
                {
                    ErrorHandler.ThrowOnFailure(bm2.AdviseUpdateSolutionEvents(this, out this._cookie2));
                }
                ErrorHandler.ThrowOnFailure(this._buildManager.AdviseUpdateSolutionEvents3(this, out this._cookie3));
            }
        }

        public void Dispose()
        {
            // Ignore failures in UnadviseSolutionEvents
            if (this._cookie1 != VSConstants.VSCOOKIE_NIL)
            {
                this._solution.UnadviseSolutionEvents(this._cookie1);
                this._cookie1 = VSConstants.VSCOOKIE_NIL;
            }
            if (this._cookie2 != VSConstants.VSCOOKIE_NIL)
            {
                ((IVsSolutionBuildManager2)this._buildManager).UnadviseUpdateSolutionEvents(this._cookie2);
                this._cookie2 = VSConstants.VSCOOKIE_NIL;
            }
            if (this._cookie3 != VSConstants.VSCOOKIE_NIL)
            {
                this._buildManager.UnadviseUpdateSolutionEvents3(this._cookie3);
                this._cookie3 = VSConstants.VSCOOKIE_NIL;
            }
        }

        int IVsUpdateSolutionEvents2.OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsUpdateSolutionEvents2.UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsUpdateSolutionEvents2.UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            var buildStarted = BuildStarted;
            if (buildStarted != null)
            {
                buildStarted(this, EventArgs.Empty);
            }
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Cancel()
        {
            return VSConstants.E_NOTIMPL;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            var buildCompleted = BuildCompleted;
            if (buildCompleted != null)
            {
                buildCompleted(this, EventArgs.Empty);
            }
            return VSConstants.S_OK;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsUpdateSolutionEvents3.OnAfterActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            var evt = ActiveSolutionConfigurationChanged;
            if (evt != null)
            {
                evt(this, EventArgs.Empty);
            }
            return VSConstants.S_OK;
        }

        int IVsUpdateSolutionEvents3.OnBeforeActiveSolutionCfgChange(IVsCfg pOldActiveSlnCfg, IVsCfg pNewActiveSlnCfg)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            var evt = SolutionClosed;
            if (evt != null)
            {
                evt(this, EventArgs.Empty);
            }
            return VSConstants.S_OK;
        }

        public int OnAfterClosingChildren(IVsHierarchy pHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterMergeSolution(object pUnkReserved)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            var project = pHierarchy as IVsProject;
            if (project != null)
            {
                var evt = ProjectLoaded;
                if (evt != null)
                {
                    evt(this, new ProjectEventArgs(project));
                }
            }
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            var evt = SolutionOpened;
            if (evt != null)
            {
                evt(this, EventArgs.Empty);
            }
            return VSConstants.S_OK;
        }

        public int OnAfterOpeningChildren(IVsHierarchy pHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            var project = pHierarchy as IVsProject;
            if (project != null)
            {
                var evt = ProjectClosing;
                if (evt != null)
                {
                    evt(this, new ProjectEventArgs(project));
                }
            }
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnBeforeClosingChildren(IVsHierarchy pHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnBeforeOpeningChildren(IVsHierarchy pHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            var project = pRealHierarchy as IVsProject;
            if (project != null)
            {
                var evt = ProjectUnloading;
                if (evt != null)
                {
                    evt(this, new ProjectEventArgs(project));
                }
            }
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterAsynchOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterChangeProjectParent(IVsHierarchy pHierarchy)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int OnAfterRenameProject(IVsHierarchy pHierarchy)
        {
            var project = pHierarchy as IVsProject;
            if (project != null)
            {
                var evt = ProjectRenamed;
                if (evt != null)
                {
                    evt(this, new ProjectEventArgs(project));
                }
            }
            return VSConstants.S_OK;
        }

        public int OnQueryChangeProjectParent(IVsHierarchy pHierarchy, IVsHierarchy pNewParentHier, ref int pfCancel)
        {
            return VSConstants.E_NOTIMPL;
        }
    }
}
