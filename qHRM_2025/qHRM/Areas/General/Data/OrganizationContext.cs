using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using qHRM.Areas.General.Models;
//using qHRM.SignalR;
using qHRM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.General.Data
{
    public class OrganizationContext
    {
        #region ==============Member Variables==============
        private readonly qHRMasterContext _hrmContext;
       
        #endregion

        #region ==============Constructors==============
        public OrganizationContext(qHRMasterContext hrmContext)
        {
            this._hrmContext = hrmContext;
        }
        #endregion

        #region ==============Org Info==============
        public IEnumerable<OrgInfo> GetOrganizations(string feild, int orgId)
        {
            try
            {
                string sp = "[ORG_Info_Select] {0},{1}";
                object[] param = { feild, orgId };
                var list = new List<OrgInfo>();
                list = _hrmContext.OrgInfo.FromSqlRaw(sp, param).ToList();
                return list;
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Entity==============
        public partial class OrgEnt
        {
            public int EntId { get; set; }
            public string NameEng { get; set; }
        }

        public IEnumerable<OrgEntity> GetAllEntities()
        {
            try
            {
                return _hrmContext.OrgEntity.Where(x => x.RecStat == 1).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public OrgEntity GetEntityByEntId(int entId)
        {
            try
            {
                return _hrmContext.OrgEntity.Where(x => x.EntId == entId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<OrgEntity> GetAllEntityFullViews()
        {
            try
            {
                return _hrmContext.OrgEntity
                        .Join(_hrmContext.OrgInfo, og => og.OrgId, en => en.OrgId, (en, og) => new { en, og })
                        .Where(z => z.en.RecStat == 1)
                        .Select(x => new OrgEntity
                        {
                            EntId = x.en.EntId,
                            EntCode = x.en.EntCode,
                            NameEng = x.en.NameEng,
                            NameArb = x.en.NameArb,
                            Desc = x.en.Desc,
                            OrgId = x.en.OrgId,
                            ex_OrgNameEng = x.og.NameEng,
                            ex_OrgNameArb = x.og.NameArb,

                            RecStat = x.en.RecStat,
                            CrtDateTime = x.en.CrtDateTime
                        }).OrderBy(p => p.EntCode);
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<OrgEnt> GetEntities()
        {
            try
            {               

                IEnumerable<OrgEnt> objFiltered = from Context in _hrmContext.OrgEntity
                                                  select new OrgEnt
                                                  {
                                                      EntId = Context.EntId,
                                                      NameEng = Context.NameEng
                                                    };
                return objFiltered;
            }
            catch (Exception ex) { throw ex; }
        }
        
   
        #endregion

        #region ==============Sys Country==============
        public partial class CntryView
        {
            public int CntryId { get; set; }
            public string CntryNameEng { get; set; }
        }

        public OrgCountry GetCountryById(int crtId)
        {
            try
            {
                return _hrmContext.OrgCountry.Where(x => x.CntryId == crtId).FirstOrDefault();
            }
            catch { throw; }
        }

        public OrgCountry GetCountryByNameEng(string nameEng)
        {
            try
            {
                return _hrmContext.OrgCountry.Where(x => x.CntryNameEng.ToLower() == nameEng.ToLower()).FirstOrDefault();
            }
            catch { throw; }
        }

        public IEnumerable<OrgCountry> GetAllCountries_N()
        {
            try
            {
                return _hrmContext.OrgCountry.Where(x => x.RecStat == 1).OrderBy(x => x.CntryNameEng).ToList();
            }
            catch { throw; }
        }

        public IEnumerable<CntryView> GetAllCntryViews()
        {
            try
            {
                return (from SysCtry in _hrmContext.OrgCountry
                       where SysCtry.RecStat == 1
                       select new CntryView
                       {
                           CntryId = SysCtry.CntryId,
                           CntryNameEng = SysCtry.CntryNameEng
                       }).ToList();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Sys Nationality Type (Grouping)==============
        public partial class NltyTypView
        {
            public int NltyTypId { get; set; }
            public string NltyTypNameEng { get; set; }
        }

        public IEnumerable<NltyTypView> GetAllNltyTypView()
        {
            try
            {
                return _hrmContext.OrgNltyType
                        .Select(x => new NltyTypView
                        {
                            NltyTypId = x.NltyTypId,
                            NltyTypNameEng = x.NltyTypNameEng
                        });
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Sys Nationality==============
        public partial class Nlty
        {
            public int NltyId { get; set; }
            public string NltyNameEng { get; set; }
            public int CntryId { get; set; }
        }

        public IEnumerable<OrgNationality> GetAllNationalities_N()
        {
            try
            {
                return _hrmContext.OrgNationality
                        .Join(_hrmContext.OrgCountry, cn => cn.CntryId, nt => nt.CntryId, (nt, cn) => new { nt, cn})
                        .Where(z => z.nt.RecStat == 1)
                        .Select(x => new OrgNationality
                        {
                           NltyId = x.nt.NltyId,
                           NltyNameEng = x.nt.NltyNameEng,
                           NltyNameArb = x.nt.NltyNameArb,
                           CntryId = x.nt.CntryId,
                           ex_CntryNameEng = x.cn.CntryNameEng,
                           Note = x.nt.Note,
                           RecStat = x.nt.RecStat,
                           CrtId = x.nt.CrtId,
                           CrtDateTime = x.nt.CrtDateTime
                        }).OrderBy(p => p.NltyNameEng);
            }
            catch (Exception ex) { throw ex; }
        }

        public OrgNationality GetNationalityById(int nltyId)
        {
            try
            {
                return _hrmContext.OrgNationality.Where(x => x.NltyId == nltyId).FirstOrDefault();
            }
            catch { throw; }
        }

        public OrgNationality GetNationalityByNameEng(string nameEng)
        {
            try
            {
                return _hrmContext.OrgNationality.Where(x => x.NltyNameEng.ToLower() == nameEng.ToLower()).FirstOrDefault();
            }
            catch { throw; }
        }

        public IEnumerable<Nlty> GetAllNationalityViews()
        {
            try
            {
                return (from OrgNationality in _hrmContext.OrgNationality
                                                where OrgNationality.RecStat == 1
                                                select new Nlty
                                                {
                                                    NltyId = OrgNationality.NltyId,
                                                    NltyNameEng = OrgNationality.NltyNameEng,
                                                    CntryId = OrgNationality.CntryId
                                                }).ToList();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Work Type==============
        public partial class WrkTypeView
        {
            public int WrktypId { get; set; }
            public string WrktypCode { get; set; }
            public string WrktypName { get; set; }
            public string NameEng { get => WrktypName; set => WrktypName = value; }
        }

        public IEnumerable<WrkTypeView> GetAllWrkTypeViews()
        {
            try
            {
                return (from wt in _hrmContext.OrgWrkType
                       where wt.RecStat == 1
                       orderby wt.WrktypId
                       select new WrkTypeView
                       {
                           WrktypId = wt.WrktypId,
                           WrktypCode = wt.WrktypCode,
                           WrktypName = wt.NameEng
                       }).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public IEnumerable<OrgWrkType> GetAllWrkTypes()
        {
            try
            {
                return _hrmContext.OrgWrkType.Where(x => x.RecStat == 1).OrderBy(x => x.WrktypId).ToList();
            }
            catch (Exception ex) { throw ex; }
        }

        public OrgWrkType GetWrkTypeById(int wrktypId)
        {
            try
            {
                return _hrmContext.OrgWrkType.Where(x => x.WrktypId == wrktypId).FirstOrDefault();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion

        #region ==============Religion==============
        public partial class ReligionView
        {
            public int RelId { get; set; }
            public string RelNameEng { get; set; }
            public string RelNameArb { get; set; }
        }

        public IEnumerable<ReligionView> GetAllReligionViews()
        {
            try
            {
                return (from rel in _hrmContext.OrgReligion
                       where rel.RecStat == 1
                       orderby rel.RelId
                       select new ReligionView
                       {
                           RelId = rel.RelId,
                           RelNameEng = rel.RelNameEng,
                           RelNameArb = rel.RelNameArb
                       }).ToList();
            }
            catch (Exception ex) { throw ex; }
        }
        #endregion
    }
}
