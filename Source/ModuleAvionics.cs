﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP;
using UnityEngine;

namespace RP0
{
    class ModuleAvionics : PartModule
    {
        #region Members
        [KSPField]
        public float massLimit = float.MaxValue; // default is unlimited

        [KSPField]
        public float enabledkW = -1f;

        [KSPField]
        public float disabledkW = -1f;

        [KSPField(guiActive = false, guiName = "Watts used: ", guiFormat = "N1", guiUnits = " W")]
        public float currentWatts = 0f;

        [KSPField]
        public bool toggleable = false;

        [KSPField(isPersistant = true)]
        public bool systemEnabled = true;

        [KSPField]
        public string techRequired = "";

        protected ModuleResource commandChargeResource = null;
        protected bool wasWarping = false;
        protected bool currentlyEnabled = true;

        // returns current limit, based on enabled/disabled
        public float CurrentMassLimit
        {
            get
            {
                if (currentlyEnabled && (string.IsNullOrEmpty(techRequired) || HighLogic.CurrentGame == null || HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX || ResearchAndDevelopment.GetTechnologyState(techRequired) == RDTech.State.Available))
                    return massLimit;
                else
                    return 0f;
            }
        }
		#endregion

		#region Utility methods
		protected double ResourceRate()
		{
			ModuleCommand mC = part.FindModuleImplementing<ModuleCommand>();

			if (mC != null)
			{
				foreach (ModuleResource r in mC.resHandler.inputResources)
				{
					if (r.id == PartResourceLibrary.ElectricityHashcode)
					{
						commandChargeResource = r;
						if (enabledkW < 0)
							enabledkW = (float)r.rate;
						return r.rate;
					}
				}
			}
			return -1;
		}

		protected void UpdateRate()
		{
			if (part.protoModuleCrew.Count > 0)
			{
				currentlyEnabled = systemEnabled = true;
				
				commandChargeResource.rate = currentWatts = enabledkW;
				ScreenMessages.PostScreenMessage("Cannot shut down avionics while crewed");
			}
			else
			{
				currentlyEnabled = !((TimeWarp.WarpMode == TimeWarp.Modes.HIGH && TimeWarp.CurrentRate > 1f) || !systemEnabled);
				if (currentlyEnabled)
				{
					commandChargeResource.rate = currentWatts = enabledkW;
				}
				else
				{
					commandChargeResource.rate = currentWatts = disabledkW;
				}
			}
			currentWatts *= 1000f;
		}

		private void SetActionsAndGui()
		{
			Events["ToggleEvent"].guiName = (systemEnabled ? "Shutdown" : "Activate") + " Avionics";
			Actions["ActivateAction"].active = !systemEnabled;
			Actions["ShutdownAction"].active = systemEnabled;
		}
        #endregion

        #region Overrides
        public void Start()
        {
            // check then bind to ModuleCommand
            if (toggleable && disabledkW >= 0f && ResourceRate() >= 0)
                UpdateRate();
            else
            {
                toggleable = false;
                currentlyEnabled = true; // just in case
            }

            Fields["currentWatts"].guiActive = 
                Events["ToggleEvent"].guiActive =
                Events["ToggleEvent"].guiActiveEditor = 
                Actions["ToggleAction"].active =
                Actions["ActivateAction"].active =
                Actions["ShutdownAction"].active = toggleable;

			SetActionsAndGui();
        }

        public override string GetInfo()
        {
            string retStr = "This part allows control of vessels of ";
            if (massLimit < float.MaxValue)
                retStr += "up to " + massLimit.ToString("N3") + " tons.";
            else
                retStr += "any mass.";
            if(toggleable && disabledkW >= 0f)
            {
                double resRate = ResourceRate();
                if(resRate >= 0)
                {
                    retStr += "\nCan be disabled, to lower command module wattage from " 
                        + (enabledkW * 1000d).ToString("N1") + " W to " + (disabledkW * 1000d).ToString("N1") + " W.";
                }
            }
            if (!string.IsNullOrEmpty(techRequired))
                retStr += "\nNote: requires technology unlock to function.";
            return retStr;
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Automatic mode switch
            bool isWarping = (TimeWarp.WarpMode == TimeWarp.Modes.HIGH && TimeWarp.CurrentRate > 1f);
            if (toggleable && isWarping != wasWarping)
            {
                // Maybe do a screenmessage here?
                UpdateRate();
            }
            wasWarping = isWarping;
        }
		#endregion

		#region Actions and Events
		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Shutdown Avionics")]
		public void ToggleEvent()
		{
			systemEnabled = !systemEnabled;
			UpdateRate();
			SetActionsAndGui();
		}

        [KSPAction("Toggle Avionics")]
        public void ToggleAction(KSPActionParam param)
        {
            ToggleEvent();
        }

        [KSPAction("Shutdown Avionics")]
        public void ShutdownAction(KSPActionParam param)
        {
            systemEnabled = true;
            ToggleEvent();
        }

        [KSPAction("Activate Avionics")]
        public void ActivateAction(KSPActionParam param)
        {
            systemEnabled = false;
            ToggleEvent();
        }
        #endregion

    }
}
