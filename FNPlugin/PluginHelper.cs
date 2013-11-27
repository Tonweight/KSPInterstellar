﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace FNPlugin {
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
	public class PluginHelper : MonoBehaviour {
        public const double FIXED_SAT_ALTITUDE = 13599840256;
        public const int REF_BODY_KERBOL = 0;
        public const int REF_BODY_KERBIN = 1;
        public const int REF_BODY_MUN = 2;
        public const int REF_BODY_MINMUS = 3;
        public const int REF_BODY_MOHO = 4;
        public const int REF_BODY_EVE = 5;
        public const int REF_BODY_DUNA = 6;
        public const int REF_BODY_IKE = 7;
        public const int REF_BODY_JOOL = 8;
        public const int REF_BODY_LAYTHE = 9;
        public const int REF_BODY_VALL = 10;
        public const int REF_BODY_BOP = 11;
        public const int REF_BODY_TYLO = 12;
        public const int REF_BODY_GILLY = 13;
        public const int REF_BODY_POL = 14;
        public const int REF_BODY_DRES = 15;
        public const int REF_BODY_EELOO = 16;
        public static string[] atomspheric_resources = {"Oxygen", "Hydrogen","Argon","Deuterium"};
        public static string[] atomspheric_resources_tocollect = { "Oxidizer", "LiquidFuel", "Argon","Deuterium"};
        public static string hydrogen_resource_name = "LiquidFuel";
        public static string oxygen_resource_name = "Oxidizer";
        public static string aluminium_resource_name = "Aluminium";
        public static string methane_resource_name = "LqdMethane";
        public static string argon_resource_name = "Argon";
        
		protected static bool plugin_init = false;
		protected static bool is_thermal_dissip_disabled_init = false;
		protected static bool is_thermal_dissip_disabled = false;
        protected static GameDatabase gdb;
        protected static bool resources_configured = false;
        protected static FloatCurve satcurve = new FloatCurve();
        
        public static string getPluginSaveFilePath() {
            return KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/WarpPlugin.cfg";
        }

        public static string getPluginSettingsFilePath() {
            return KSPUtil.ApplicationRootPath + "GameData/WarpPlugin/WarpPluginSettings.cfg";
        }

		public static bool isThermalDissipationDisabled() {
			return is_thermal_dissip_disabled;
		}

		public static bool hasTech(string techid) {
			try{
				string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
				ConfigNode config = ConfigNode.Load (persistentfile);
				ConfigNode gameconf = config.GetNode ("GAME");
				ConfigNode[] scenarios = gameconf.GetNodes ("SCENARIO");
				foreach (ConfigNode scenario in scenarios) {
					if (scenario.GetValue ("name") == "ResearchAndDevelopment") {
						ConfigNode[] techs = scenario.GetNodes ("Tech");
						foreach (ConfigNode technode in techs) {
							if (technode.GetValue ("id") == techid) {
								return true;
							}
						}
					}
				}
				return false;
			} catch (Exception ex) {
				return false;
			}
		}

		public static float getKerbalRadiationDose(int kerbalidx) {
			try{
				string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
				ConfigNode config = ConfigNode.Load (persistentfile);
				ConfigNode gameconf = config.GetNode ("GAME");
				ConfigNode crew_roster = gameconf.GetNode("ROSTER");
				ConfigNode[] crew = crew_roster.GetNodes("CREW");
				ConfigNode sought_kerbal = crew[kerbalidx];
				if(sought_kerbal.HasValue("totalDose")) {
					float dose = float.Parse(sought_kerbal.GetValue("totalDose"));
					return dose;
				}
				return 0.0f;
			}catch (Exception ex) {
				print (ex);
				return 0.0f;
			}
		}

		public static ConfigNode getKerbal(int kerbalidx) {
			try{
				string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
				ConfigNode config = ConfigNode.Load (persistentfile);
				ConfigNode gameconf = config.GetNode ("GAME");
				ConfigNode crew_roster = gameconf.GetNode("ROSTER");
				ConfigNode[] crew = crew_roster.GetNodes("CREW");
				ConfigNode sought_kerbal = crew[kerbalidx];
				return sought_kerbal;
			}catch (Exception ex) {
				print (ex);
				return null;
			}
		}

		public static void saveKerbalRadiationdose(int kerbalidx, float rad) {
			try{
				string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
				ConfigNode config = ConfigNode.Load (persistentfile);
				ConfigNode gameconf = config.GetNode ("GAME");
				ConfigNode crew_roster = gameconf.GetNode("ROSTER");
				ConfigNode[] crew = crew_roster.GetNodes("CREW");
				ConfigNode sought_kerbal = crew[kerbalidx];
				if(sought_kerbal.HasValue("totalDose")) {
					sought_kerbal.SetValue("totalDose",rad.ToString("E"));
				}else{
					sought_kerbal.AddValue("totalDose",rad.ToString("E"));
				}
				config.Save(persistentfile);
			}catch (Exception ex) {
				print (ex);
			}
		}

		public static ConfigNode getPluginSaveFile() {
			ConfigNode config = ConfigNode.Load (PluginHelper.getPluginSaveFilePath ());
			if (config == null) {
				config = new ConfigNode ();
				config.AddValue("writtenat",DateTime.Now.ToString());
				config.Save(PluginHelper.getPluginSaveFilePath ());
			}
			return config;
		}

        public static ConfigNode getPluginSettingsFile() {
            ConfigNode config = ConfigNode.Load(PluginHelper.getPluginSettingsFilePath());
            if (config == null) {
                config = new ConfigNode();
            }
            return config;
        }

        public static bool lineOfSightToSun(Vessel vess) {
            Vector3d a = vess.transform.position;
            Vector3d b = FlightGlobals.Bodies[0].transform.position;
            foreach (CelestialBody referenceBody in FlightGlobals.Bodies) {
                if (referenceBody.flightGlobalsIndex == 0) { // the sun should not block line of sight to the sun
                    continue;
                }
                Vector3d refminusa = referenceBody.position - a;
                Vector3d bminusa = b - a;
                if (Vector3d.Dot(refminusa, bminusa) > 0) {
                    if (Vector3d.Dot(refminusa, bminusa.normalized) < bminusa.magnitude) {
                        Vector3d tang = refminusa - Vector3d.Dot(refminusa, bminusa.normalized) * bminusa.normalized;
                        if (tang.magnitude < referenceBody.Radius) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public static FloatCurve getSatFloatCurve() {
            return satcurve;
        }

        public static float getAtmosphereResourceContent(int refBody, int resource) {
            float resourcecontent = 0;
            if (refBody == REF_BODY_KERBIN) {
                if (resource == 0) {
                    resourcecontent = 0.21f;
                }
                if (resource == 2) {
                    resourcecontent = 0.0093f;
                }
            }

            if (refBody == REF_BODY_LAYTHE) {
                if (resource == 0) {
                    resourcecontent = 0.18f;
                }
                if (resource == 2) {
                    resourcecontent = 0.0105f;
                }
            }

            if (refBody == REF_BODY_JOOL) {
                if (resource == 1) {
                    resourcecontent = 0.89f;
                }
				if (resource == 3) {
					resourcecontent = 0.00003f;
				}
            }

            if (refBody == REF_BODY_DUNA) {
                if (resource == 0) {
                    resourcecontent = 0.0013f;
                }
                if (resource == 2) {
                    resourcecontent = 0.0191f;
                }
            }

            if (refBody == REF_BODY_EVE) {
                if (resource == 2) {
                    resourcecontent = 0.00007f;
                }
            }

            return resourcecontent;
        }

		public static float getMaxAtmosphericAltitude(CelestialBody body) {
			if (!body.atmosphere) {
				return 0;
			}
			return (float) -body.atmosphereScaleHeight * 1000.0f * Mathf.Log(1e-6f);
		}

        public static float getScienceMultiplier(int refbody, bool landed) {
			float multiplier = 1;

			if (refbody == REF_BODY_DUNA || refbody == REF_BODY_EVE || refbody == REF_BODY_IKE || refbody == REF_BODY_GILLY) {
				multiplier = 5f;
			} else if (refbody == REF_BODY_MUN || refbody == REF_BODY_MINMUS) {
				multiplier = 2.5f;
			} else if (refbody == REF_BODY_JOOL || refbody == REF_BODY_TYLO || refbody == REF_BODY_POL || refbody == REF_BODY_BOP) {
				multiplier = 10f;
			} else if (refbody == REF_BODY_LAYTHE || refbody == REF_BODY_VALL) {
				multiplier = 12f;
			} else if (refbody == REF_BODY_EELOO || refbody == REF_BODY_MOHO) {
				multiplier = 20f;
			} else if (refbody == REF_BODY_DRES) {
				multiplier = 7.5f;
			} else if (refbody == REF_BODY_KERBIN) {
				multiplier = 1f;
			} else if (refbody == REF_BODY_KERBOL) {
				multiplier = 15f;
			}else {
				multiplier = 0f;
			}

			if (landed) {
				if (refbody == REF_BODY_TYLO) {
					multiplier = multiplier*3f;
				} else if (refbody == REF_BODY_EVE) {
					multiplier = multiplier*2.5f;
				} else {
					multiplier = multiplier*2f;
				}
			}

            return multiplier;
        }

        public void loadPluginResourceConfig() {

        }

		public void Update() {
            this.enabled = true;
            AvailablePart intakePart = PartLoader.getPartInfoByName("CircularIntake");
            if (intakePart.partPrefab.FindModulesImplementing<AtmosphericIntake>().Count <= 0 && PartLoader.Instance.IsReady()) {
                plugin_init = false;
            }

            if (!resources_configured) {
                resources_configured = true;
                ConfigNode plugin_settings = GameDatabase.Instance.GetConfigNode("GameData/WarpPlugin/WarpPluginSettings/WARP_PLUGIN_SETTINGS");
                if(plugin_settings != null) {
                    if (plugin_settings.HasValue("HydrogenResourceName")) {
                        PluginHelper.hydrogen_resource_name = plugin_settings.GetValue("HydrogenResourceName");
                        PluginHelper.atomspheric_resources_tocollect[1] = PluginHelper.hydrogen_resource_name;
                    }
                    if (plugin_settings.HasValue("OxygenResourceName")) {
                        PluginHelper.oxygen_resource_name = plugin_settings.GetValue("OxygenResourceName");
                        PluginHelper.atomspheric_resources_tocollect[0] = PluginHelper.oxygen_resource_name;
                    }
                    if (plugin_settings.HasValue("AluminiumResourceName")) {
                        PluginHelper.aluminium_resource_name = plugin_settings.GetValue("AluminiumResourceName");
                    }
                    if (plugin_settings.HasValue("MethaneResourceName")) {
                        PluginHelper.methane_resource_name = plugin_settings.GetValue("MethaneResourceName");
                    }
                    if (plugin_settings.HasValue("ArgonResourceName")) {
                        PluginHelper.argon_resource_name = plugin_settings.GetValue("ArgonResourceName");
                        PluginHelper.atomspheric_resources_tocollect[2] = PluginHelper.argon_resource_name;
                    }
                    if (plugin_settings.HasValue("ThermalMechanicsDisabled")) {
                        PluginHelper.is_thermal_dissip_disabled = bool.Parse(plugin_settings.GetValue("ThermalMechanicsDisabled"));
                    }
                }
                satcurve = new FloatCurve();
                satcurve.Add((float)(16.0 * Vector3d.Distance(FlightGlobals.Bodies[REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[REF_BODY_KERBOL].transform.position)), 0.00390625f, 0, 0);
                satcurve.Add((float)(8.0 * Vector3d.Distance(FlightGlobals.Bodies[REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[REF_BODY_KERBOL].transform.position)), 0.015625f, 0, 0);
                satcurve.Add((float)(4.0 * Vector3d.Distance(FlightGlobals.Bodies[REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[REF_BODY_KERBOL].transform.position)), 0.0625f, 0, 0);
                satcurve.Add((float)(2.0 * Vector3d.Distance(FlightGlobals.Bodies[REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[REF_BODY_KERBOL].transform.position)), 0.25f, 0, 0);
                satcurve.Add((float)(1.5 * Vector3d.Distance(FlightGlobals.Bodies[REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[REF_BODY_KERBOL].transform.position)), 0.4444444f, 0, 0);
                satcurve.Add((float)(1.0 * Vector3d.Distance(FlightGlobals.Bodies[REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[REF_BODY_KERBOL].transform.position)), 1.0f, 0, 0);
                satcurve.Add((float)(0.75 * Vector3d.Distance(FlightGlobals.Bodies[REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[REF_BODY_KERBOL].transform.position)), 1.777778f, 0, 0);
                satcurve.Add((float)(0.5 * Vector3d.Distance(FlightGlobals.Bodies[REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[REF_BODY_KERBOL].transform.position)), 4, 0, 0);
                satcurve.Add((float)(0.25 * Vector3d.Distance(FlightGlobals.Bodies[REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[REF_BODY_KERBOL].transform.position)), 16, 0, 0);
                satcurve.Add((float)(0.125 * Vector3d.Distance(FlightGlobals.Bodies[REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[REF_BODY_KERBOL].transform.position)), 64, 0, 0);
                satcurve.Add((float)(0.0625 * Vector3d.Distance(FlightGlobals.Bodies[REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[REF_BODY_KERBOL].transform.position)), 256, 0, 0);
                satcurve.Add((float)(0.03125 * Vector3d.Distance(FlightGlobals.Bodies[REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[REF_BODY_KERBOL].transform.position)), 1024, 0, 0);
            }

			if (!plugin_init) {
                gdb = GameDatabase.Instance;
				plugin_init = true;

                AvailablePart kerbalRadiationPart = PartLoader.getPartInfoByName("kerbalEVA");
                kerbalRadiationPart.partPrefab.gameObject.AddComponent<FNModuleRadiation>();

				List<AvailablePart> available_parts = PartLoader.LoadedPartsList;
				foreach (AvailablePart available_part in available_parts) {
					Part prefab_available_part = available_part.partPrefab;
					try {
						if(prefab_available_part.Modules != null) {
														
							if(prefab_available_part.FindModulesImplementing<ModuleResourceIntake>().Count > 0) {
								ModuleResourceIntake intake = prefab_available_part.Modules["ModuleResourceIntake"] as ModuleResourceIntake;
								if(intake.resourceName == "IntakeAir") {
									Type type = AssemblyLoader.GetClassByName(typeof(PartModule), "AtmosphericIntake");
									AtmosphericIntake pm = null;
									if(type != null) {
										pm = prefab_available_part.gameObject.AddComponent(type) as AtmosphericIntake;
										prefab_available_part.Modules.Add(pm);
										pm.area = intake.area*intake.unitScalar*intake.maxIntakeSpeed/20;
									}

									PartResource intake_air_resource = prefab_available_part.Resources["IntakeAir"];

                                    if (intake_air_resource != null && !prefab_available_part.Resources.Contains("IntakeAtm")) {
										ConfigNode node = new ConfigNode("RESOURCE");
										node.AddValue("name", "IntakeAtm");
										node.AddValue("maxAmount", intake_air_resource.maxAmount);
										node.AddValue("amount", intake_air_resource.amount);
										prefab_available_part.AddResource(node);
									}
								}

							}

                            if (prefab_available_part.FindModulesImplementing<ModuleDeployableSolarPanel>().Count > 0) {
                                ModuleDeployableSolarPanel panel = prefab_available_part.Modules["ModuleDeployableSolarPanel"] as ModuleDeployableSolarPanel;
                                if (panel.chargeRate > 0) {
                                    Type type = AssemblyLoader.GetClassByName(typeof(PartModule), "FNSolarPanelWasteHeatModule");
                                    FNSolarPanelWasteHeatModule pm = null;
                                    if (type != null) {
                                        pm = prefab_available_part.gameObject.AddComponent(type) as FNSolarPanelWasteHeatModule;
                                        prefab_available_part.Modules.Add(pm);
                                    }
                                }
                                
                                                                
                                if (!prefab_available_part.Resources.Contains("WasteHeat") && panel.chargeRate > 0) {
                                    ConfigNode node = new ConfigNode("RESOURCE");
                                    node.AddValue("name", "WasteHeat");
                                    node.AddValue("maxAmount", panel.chargeRate * 100);
                                    node.AddValue("amount", 0);
                                    PartResource pr = prefab_available_part.AddResource(node);

                                    if (available_part.resourceInfo != null && pr != null) {
                                        if (available_part.resourceInfo.Length == 0) {
                                            available_part.resourceInfo = pr.resourceName + ":" + pr.amount + " / " + pr.maxAmount;
                                        } else {
                                            available_part.resourceInfo = available_part.resourceInfo + "\n" + pr.resourceName + ":" + pr.amount + " / " + pr.maxAmount;
                                        }
                                    }
                                }

                            }

							if(prefab_available_part.FindModulesImplementing<ElectricEngineController>().Count() > 0) {
								available_part.moduleInfo = prefab_available_part.FindModulesImplementing<ElectricEngineController>().First().GetInfo();
							}

							if(prefab_available_part.FindModulesImplementing<FNNozzleController>().Count() > 0) {
								available_part.moduleInfo = prefab_available_part.FindModulesImplementing<FNNozzleController>().First().GetInfo();
							}
                            
							if(prefab_available_part.CrewCapacity > 0) {
								Type type = AssemblyLoader.GetClassByName(typeof(PartModule), "FNModuleRadiation");
								FNModuleRadiation pm = null;
								if(type != null) {
									pm = prefab_available_part.gameObject.AddComponent(type) as FNModuleRadiation;
									prefab_available_part.Modules.Add(pm);
									double rad_hardness = prefab_available_part.mass /((double)prefab_available_part.CrewCapacity)*7.5;
									pm.rad_hardness = rad_hardness;
								}
							}
						}
						//String path11 = KSPUtil.ApplicationRootPath + "GameData/WarpPlugin/Additions/" + available_part.name + ".cfg";
						//String path21 = KSPUtil.ApplicationRootPath + "GameData/WarpPlugin/Replacements/" + available_part.name + ".cfg";
						String path1 = "WarpPlugin/Additions/" + available_part.name + "/" + available_part.name;
						String path2 = "WarpPlugin/Replacements/" + available_part.name + "/" + available_part.name;

						//ConfigNode config_addition = ConfigNode.Load (path1);
						//ConfigNode config_replacement = ConfigNode.Load(path2);
						ConfigNode config_addition = null;
						ConfigNode config_replacement  = null;

						if(GameDatabase.Instance.ExistsConfigNode(path1)) {
							config_addition = GameDatabase.Instance.GetConfigNode(path1);
						}
						if(GameDatabase.Instance.ExistsConfigNode(path2)) {
							config_replacement = GameDatabase.Instance.GetConfigNode(path2);
						}
						List<ConfigNode> config_nodes = new List<ConfigNode>();
						//ConfigNode.ConfigNodeList config_nodes = new ConfigNode.ConfigNodeList();

						if(config_replacement != null) {

							foreach(ConfigNode conf_node in config_replacement.nodes) {
								config_nodes.Add(conf_node);
							}

							foreach(PartModule pm in prefab_available_part.Modules) {
								prefab_available_part.Modules.Remove(pm);
							}

							available_part.moduleInfo = "";
							available_part.resourceInfo = "";
						}

						if(config_addition != null) {
							foreach(ConfigNode conf_node in config_addition.nodes) {
								config_nodes.Add(conf_node);
							}
						}

						if(config_nodes.Count > 0) {
							print ("[WarpPlugin] PartLoader making update to : " + prefab_available_part.name + " part");
						}

						foreach (ConfigNode config_part_item in config_nodes) {
							if(config_part_item != null) {
								if(config_part_item.name == "RESOURCE") {
									PartResource pr = prefab_available_part.AddResource(config_part_item);
									if(available_part.resourceInfo != null && pr != null) {	
										if(available_part.resourceInfo.Length == 0) {
											available_part.resourceInfo = pr.resourceName + ":" + pr.amount + " / " + pr.maxAmount;
										}else{
											available_part.resourceInfo = available_part.resourceInfo + "\n" + pr.resourceName + ":" + pr.amount + " / " + pr.maxAmount;
										}
									}
								}else if(config_part_item.name == "MODULE") {
									//print ("[WarpPlugin] blah: " + prefab_available_part.name + " " + config_part_item.GetValue("name"));

									Type type = AssemblyLoader.GetClassByName(typeof(PartModule), config_part_item.GetValue("name"));

									PartModule pm = null;
									if(type != null) {
										pm = prefab_available_part.gameObject.AddComponent(type) as PartModule;
										prefab_available_part.Modules.Add(pm);
									}

									//print ("[WarpPlugin] blahblah: " + prefab_available_part.name);
									if(available_part.moduleInfo != null && pm != null) {	
										if(available_part.moduleInfo.Length == 0) {
											if(pm.GetInfo().Length >0) {
												available_part.moduleInfo = pm.GetInfo();
											}
										}else{
											if(pm.GetInfo().Length >0) {
												available_part.moduleInfo = available_part.moduleInfo + "\n" + pm.GetInfo();
											}
										}
									}
									//print ("[WarpPlugin] blahblahblah: " + prefab_available_part.name);
								}
							}
						}

					}catch(Exception ex) {
						print ("[WarpPlugin] Exception caught adding to: " + prefab_available_part.name + " part: " + ex.ToString());
					}


				}
			}

			//Destroy (this);
		}
        		
		protected static bool warning_displayed = false;

		public static void showInstallationErrorMessage() {
			if (!warning_displayed) {
				PopupDialog.SpawnPopupDialog ("KSP Interstellar Installation Error", "KSP Interstellar is unable to detect files required for the functioning of this rocket.  Please make sure that this mod has been installed to [Base KSP directory]/GameData/WarpPlugin.", "OK", false, HighLogic.Skin);
				warning_displayed = true;
			}
		}

        

        
    }
}
