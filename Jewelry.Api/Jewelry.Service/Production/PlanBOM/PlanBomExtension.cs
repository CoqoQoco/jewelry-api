using Jewelry.Data.Models.Jewelry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.PlanBOM
{
    public static class PlanBomExtension
    {
        public static string GetMasterGem(this string name, List<TbmGem> master, string defaultName)
        {
            var _name = name.Trim().ToUpper();
            var res = master.FirstOrDefault(x => _name.ToLower().Contains(x.NameEn.ToLower()))?.NameEn ?? null;

            if (string.IsNullOrEmpty(res))
            {
                if (_name.Contains("DIA"))
                {
                    res = "Diamond";
                }

                else if (_name.Contains("CZ"))
                {
                    res = "Cubic Zirconia";
                }

                else if (_name.Contains("RU"))
                {
                    res = "Ruby";
                }

                else if (_name.Contains("SAP"))
                {
                    res = "Sapphire";
                }
                else if (_name.Contains("PINK SAP"))
                {
                    res = "Pink Sapphire";
                }
                else if (_name.Contains("PINK SAP"))
                {
                    res = "Yellow Sapphire";
                }
                else if (_name.Contains("FANCY SAP"))
                {
                    res = "Fancy Sapphire";
                }
                else if (_name.Contains("YELLOW SAP"))
                {
                    res = "Yellow Sapphire";
                }
                else if (_name.Contains("WHITE SAP"))
                {
                    res = "White Sapphire";
                }

                else if (_name.Contains("AME"))
                {
                    res = "Amethyst";
                }

                else if (_name.Contains("EME"))
                {
                    res = "Emerald";
                }

                else if (_name.Contains("BLUE TO") || _name.Contains("BT"))
                {
                    res = "Blue Topaz";
                }
                else if (_name.Contains("GREEN TO"))
                {
                    res = "Green Topaz";
                }


                else if (_name.Contains("CIT") || _name.Contains("CL"))
                {
                    res = "Citrine";
                }

                else if (_name.Contains("TANZ"))
                {
                    res = "Tanzanite";
                }

                else if (_name.Contains("MIX"))
                {
                    res = "Mix Stone";
                }

                else if (_name.Contains("AQU"))
                {
                    res = "Aquamarine";
                }

                else if (_name.Contains("LEMON"))
                {
                    res = "Lemon Quart";
                }
                else if (_name.Contains("ROSE QU"))
                {
                    res = "Rose Quart";
                }
                else if (_name.Contains("WHITE QU"))
                {
                    res = "White Quart";
                }

                else if (_name.Contains("TSA"))
                {
                    res = "Tsavolite";
                }

                else if (_name.Contains("GAR"))
                {
                    res = "Garnet";
                }

                else if (_name.Contains("WHITE PEA"))
                {
                    res = "WHITE Pearl";
                }

                else
                {
                    res = defaultName; // Default name if no match found
                }

            }

            return res;
        }
    }
}
