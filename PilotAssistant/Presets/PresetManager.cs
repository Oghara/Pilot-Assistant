﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PilotAssistant.Presets
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    internal class PresetManager : MonoBehaviour
    {
        internal static PresetPA defaultPATuning;
        
        internal static List<PresetPA> PAPresetList = new List<PresetPA>();

        internal static PresetPA activePAPreset = null;

        internal static PresetSAS defaultSASTuning;
        internal static PresetSAS defaultStockSASTuning;

        internal static List<PresetSAS> SASPresetList = new List<PresetSAS>();

        internal static PresetSAS activeSASPreset = null;
        internal static PresetSAS activeStockSASPreset = null;

        internal static List<CraftPreset> craftPresetList = new List<CraftPreset>();

        public void Start()
        {
            loadPresetsFromFile();
            DontDestroyOnLoad(this);
        }

        public void OnDestroy()
        {
            saveToFile();
        }

        internal static void loadPresetsFromFile()
        {
            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("PIDPreset"))
            {
                if (node == null)
                    continue;
                
                List<double[]> gains = new List<double[]>();
                gains.Add(controllerGains(node.GetNode("HdgBankController")));
                gains.Add(controllerGains(node.GetNode("HdgYawController")));
                gains.Add(controllerGains(node.GetNode("AileronController")));
                gains.Add(controllerGains(node.GetNode("RudderController")));
                gains.Add(controllerGains(node.GetNode("AltitudeController")));
                gains.Add(controllerGains(node.GetNode("AoAController")));
                gains.Add(controllerGains(node.GetNode("ElevatorController")));
                PAPresetList.Add(new PresetPA(gains, node.GetValue("name")));
            }

            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("SASPreset"))
            {
                if (node == null)
                    continue;

                List<double[]> gains = new List<double[]>();
                gains.Add(controllerSASGains(node.GetNode("AileronController")));
                gains.Add(controllerSASGains(node.GetNode("RudderController")));
                gains.Add(controllerSASGains(node.GetNode("ElevatorController")));
                SASPresetList.Add(new PresetSAS(gains, node.GetValue("name"), bool.Parse(node.GetValue("stock"))));
            }

            foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("CraftPreset"))
            {
                if (node == null)
                    continue;

                CraftPreset cP = new CraftPreset(node.GetValue("name"),
                                        PAPresetList.FirstOrDefault(p => p.name == node.GetValue("pilot")),
                                        SASPresetList.FirstOrDefault(p => p.name == node.GetValue("ssas")),
                                        SASPresetList.FirstOrDefault(p => p.name == node.GetValue("stock")));
            }
        }

        internal static void saveToFile()
        {
            ConfigNode node = new ConfigNode();
            if (PAPresetList.Count == 0 && SASPresetList.Count == 0 && craftPresetList.Count == 0)
                node.AddValue("dummy", "do not delete me");
            else
            {
                foreach (PresetPA p in PAPresetList)
                {
                    node.AddNode(PAPresetNode(p));
                }
                foreach (PresetSAS p in SASPresetList)
                {
                    node.AddNode(SASPresetNode(p));
                }
                foreach (CraftPreset c in craftPresetList)
                {
                    node.AddNode(CraftNode(c));
                }
            }
            node.Save(KSPUtil.ApplicationRootPath.Replace("\\", "/") + "GameData/Pilot Assistant/Presets.cfg");
        }

        private static double[] controllerGains(ConfigNode node)
        {
            double[] gains = new double[8];
            double.TryParse(node.GetValue("PGain"), out gains[0]);
            double.TryParse(node.GetValue("IGain"), out gains[1]);
            double.TryParse(node.GetValue("DGain"), out gains[2]);
            double.TryParse(node.GetValue("MinOut"), out gains[3]);
            double.TryParse(node.GetValue("MaxOut"), out gains[4]);
            double.TryParse(node.GetValue("ClampLower"), out gains[5]);
            double.TryParse(node.GetValue("ClampUpper"), out gains[6]);
            double.TryParse(node.GetValue("Scalar"), out gains[7]);

            return gains;
        }

        private static double[] controllerSASGains(ConfigNode node)
        {
            double[] gains = new double[4];
            double.TryParse(node.GetValue("PGain"), out gains[0]);
            double.TryParse(node.GetValue("IGain"), out gains[1]);
            double.TryParse(node.GetValue("DGain"), out gains[2]);
            double.TryParse(node.GetValue("Scalar"), out gains[3]);

            return gains;
        }

        private static ConfigNode PAPresetNode(PresetPA preset)
        {
            ConfigNode node = new ConfigNode("PIDPreset");
            node.AddValue("name", preset.name);
            node.AddNode(PIDnode("HdgBankController", (int)PIDList.HdgBank, preset));
            node.AddNode(PIDnode("HdgYawController", (int)PIDList.HdgYaw, preset));
            node.AddNode(PIDnode("AileronController", (int)PIDList.Aileron, preset));
            node.AddNode(PIDnode("RudderController", (int)PIDList.Rudder, preset));
            node.AddNode(PIDnode("AltitudeController", (int)PIDList.Altitude, preset));
            node.AddNode(PIDnode("AoAController", (int)PIDList.VertSpeed, preset));
            node.AddNode(PIDnode("ElevatorController", (int)PIDList.Elevator, preset));

            return node;
        }

        private static ConfigNode SASPresetNode(PresetSAS preset)
        {
            ConfigNode node = new ConfigNode("SASPreset");
            node.AddValue("name", preset.name);
            node.AddValue("stock", preset.bStockSAS);
            node.AddNode(PIDnode("AileronController", (int)SASList.Roll, preset));
            node.AddNode(PIDnode("RudderController", (int)SASList.Yaw, preset));
            node.AddNode(PIDnode("ElevatorController", (int)SASList.Pitch, preset));

            return node;
        }

        private static ConfigNode CraftNode(CraftPreset preset)
        {
            ConfigNode node = new ConfigNode("CraftPreset");
            node.AddValue("name", preset.Name);
            node.AddValue("pilot", preset.PresetPA);
            node.AddValue("ssas", preset.SSAS);
            node.AddValue("stock", preset.Stock);

            return node;
        }

        private static ConfigNode PIDnode(string name, int index, PresetPA preset)
        {
            ConfigNode node = new ConfigNode(name);
            node.AddValue("PGain", preset.PIDGains[index][0]);
            node.AddValue("IGain", preset.PIDGains[index][1]);
            node.AddValue("DGain", preset.PIDGains[index][2]);
            node.AddValue("MinOut", preset.PIDGains[index][3]);
            node.AddValue("MaxOut", preset.PIDGains[index][4]);
            node.AddValue("ClampLower", preset.PIDGains[index][5]);
            node.AddValue("ClampUpper", preset.PIDGains[index][6]);
            node.AddValue("Scalar", preset.PIDGains[index][7]);
            return node;
        }

        private static ConfigNode PIDnode(string name, int index, PresetSAS preset)
        {
            ConfigNode node = new ConfigNode(name);
            node.AddValue("PGain", preset.PIDGains[index][0]);
            node.AddValue("IGain", preset.PIDGains[index][1]);
            node.AddValue("DGain", preset.PIDGains[index][2]);
            node.AddValue("Scalar", preset.PIDGains[index][3]);
            return node;
        }

        internal static void loadPAPreset(PresetPA p)
        {
            List<PID.PID_Controller> c = PilotAssistant.controllers;

            for (int i = 0; i < 7; i++)
            {
                c[i].PGain = p.PIDGains[i][0];
                c[i].IGain = p.PIDGains[i][1];
                c[i].DGain = p.PIDGains[i][2];
                c[i].OutMin = p.PIDGains[i][3];
                c[i].OutMax = p.PIDGains[i][4];
                c[i].ClampLower = p.PIDGains[i][5];
                c[i].ClampUpper = p.PIDGains[i][6];
                c[i].Scalar = p.PIDGains[i][7];
            }
        }

        internal static void loadSASPreset(PresetSAS p)
        {
            List<PID.PID_Controller> c = SurfSAS.SASControllers;

            for (int i = 0; i < 3; i++)
            {
                c[i].PGain = p.PIDGains[i][0];
                c[i].IGain = p.PIDGains[i][1];
                c[i].DGain = p.PIDGains[i][2];
                c[i].Scalar = p.PIDGains[i][3];
            }
        }

        internal static void loadStockSASPreset(PresetSAS p)
        {
            Utility.FlightData.thisVessel.Autopilot.SAS.pidLockedPitch.kp = p.PIDGains[(int)SASList.Pitch][0];
            Utility.FlightData.thisVessel.Autopilot.SAS.pidLockedPitch.ki = p.PIDGains[(int)SASList.Pitch][1];
            Utility.FlightData.thisVessel.Autopilot.SAS.pidLockedPitch.kd = p.PIDGains[(int)SASList.Pitch][2];
            Utility.FlightData.thisVessel.Autopilot.SAS.pidLockedPitch.clamp = p.PIDGains[(int)SASList.Pitch][3];

            Utility.FlightData.thisVessel.Autopilot.SAS.pidLockedRoll.kp = p.PIDGains[(int)SASList.Roll][0];
            Utility.FlightData.thisVessel.Autopilot.SAS.pidLockedRoll.ki = p.PIDGains[(int)SASList.Roll][1];
            Utility.FlightData.thisVessel.Autopilot.SAS.pidLockedRoll.kd = p.PIDGains[(int)SASList.Roll][2];
            Utility.FlightData.thisVessel.Autopilot.SAS.pidLockedRoll.clamp = p.PIDGains[(int)SASList.Roll][3];

            Utility.FlightData.thisVessel.Autopilot.SAS.pidLockedYaw.kp = p.PIDGains[(int)SASList.Yaw][0];
            Utility.FlightData.thisVessel.Autopilot.SAS.pidLockedYaw.ki = p.PIDGains[(int)SASList.Yaw][1];
            Utility.FlightData.thisVessel.Autopilot.SAS.pidLockedYaw.kd = p.PIDGains[(int)SASList.Yaw][2];
            Utility.FlightData.thisVessel.Autopilot.SAS.pidLockedYaw.clamp = p.PIDGains[(int)SASList.Yaw][3];
        }
    }
}
