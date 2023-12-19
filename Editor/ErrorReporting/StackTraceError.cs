﻿using System;
using System.Runtime.InteropServices;
using nadena.dev.ndmf.localization;
using nadena.dev.ndmf.ui;
using UnityEngine.UIElements;

namespace nadena.dev.ndmf
{
    public class StackTraceError : SimpleError
    {
        private Exception _e;
        private string _stackTrace;
        
        public Exception Exception => _e;
        
        public StackTraceError(Exception e, string additionalStackTrace = null)
        {
            this._e = e;

            this._stackTrace = _e.StackTrace != null ? ("\n" + _e.StackTrace) : "";

            if (additionalStackTrace != null)
            {
                this._stackTrace += "\n" + additionalStackTrace;
            }
        }

        protected override Localizer Localizer => NDMFLocales.L;
        protected override string TitleKey => "Errors:InternalError";
        public override ErrorCategory Category => ErrorCategory.InternalError;

        protected override string[] DetailsSubst => new []
        {
            _e.GetType().Name
        };

        public override VisualElement CreateVisualElement(ErrorReport report)
        {
            SimpleErrorUI ui = (SimpleErrorUI) base.CreateVisualElement(report);
            if (_e.StackTrace != null) ui.AddStackTrace(_e + _stackTrace);
            else ui.AddStackTrace(_e.ToString());
            return ui;
        }

        public override string ToMessage()
        {
            return base.ToMessage() + "\n\n" + _e + _stackTrace;
            
        }
    }
}