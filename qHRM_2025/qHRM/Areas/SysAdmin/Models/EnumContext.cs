using System;
using System.Collections.Generic;

namespace qHRM.Areas.SysAdmin.Models
{
    public class EnumContext
    {
        public partial class Title { public string title { get; set; } }
        public partial class Gender { public string gender { get; set; } }        
        public partial class EmpTyp { public string emptyp { get; set; } }
        public partial class PmtTyp { public string pmttyp { get; set; } }
        public partial class DpndPmtTyp { public string dpndpmttyp { get; set; } }
        public partial class DpndDocTyp { public int dpnddocid { get; set; } public string dpnddocname { get; set; } }
        public partial class MrtlStat { public string mrtlstat { get; set; } }
        public partial class BldGrp { public string bldgrp { get; set; } }
        public partial class CurrStat { public int currid { get; set; }  public string currstat { get; set; } }        
        public partial class AcctTyp { public string accttype { get; set; }}
        public partial class Occup { public string occp { get; set; } }
        public partial class ResType { public string restype { get; set; } }



        public List<Title> lstTitle = new List<Title>()
        {
            new Title() {title = "Mr."},
            new Title() {title = "Mrs."},
            new Title() {title = "Ms."},
            new Title() {title = "Mast."}
        };

        public List<Gender> lstGender = new List<Gender>()
        {
            new Gender() {gender = "Male"},
            new Gender() {gender = "Female"}
        };        

            public List<EmpTyp> lstEmpTyp = new List<EmpTyp>()
        {
            new EmpTyp() {emptyp="Permanent"},
            new EmpTyp() {emptyp="Contract"},
            new EmpTyp() {emptyp="Temporary"}
        };

        public List<PmtTyp> lstPmtTyp = new List<PmtTyp>()
        {
            new PmtTyp() {pmttyp="Residence Permit"},
            new PmtTyp() {pmttyp="Business Visa"},
            new PmtTyp() {pmttyp="Tourist Visa"}
        };

        public List<DpndPmtTyp> lstDpdPmtTyp = new List<DpndPmtTyp>()
        {
            new DpndPmtTyp() {dpndpmttyp="None"},
            new DpndPmtTyp() {dpndpmttyp="Parent"},
            new DpndPmtTyp() {dpndpmttyp="Other Sponsor"}
        };


        public List<DpndDocTyp> lstDpdDocTyp = new List<DpndDocTyp>()
        {
            new DpndDocTyp() {dpnddocid =1, dpnddocname="QID"},
            new DpndDocTyp() {dpnddocid =2, dpnddocname="Visa"},
            new DpndDocTyp() {dpnddocid =3, dpnddocname="Passport"},
            new DpndDocTyp() {dpnddocid =4, dpnddocname="Birth Certificate"},
            new DpndDocTyp() {dpnddocid =5, dpnddocname="Marraige Certificate"},
            new DpndDocTyp() {dpnddocid =5, dpnddocname="Educational Certificate"},
            new DpndDocTyp() {dpnddocid =6, dpnddocname="Other"}
        };


        public List<MrtlStat> lstMrtlStat = new List<MrtlStat>()
        {
            new MrtlStat() {mrtlstat="Married"},
            new MrtlStat() {mrtlstat="Single"},
            new MrtlStat() {mrtlstat="Divorced"},
            new MrtlStat() {mrtlstat="Widowed"}
        };        

        public List<BldGrp> lstBldGrp = new List<BldGrp>()
        {
            new BldGrp() {bldgrp = "A+"},
            new BldGrp() {bldgrp = "A-"},
            new BldGrp() {bldgrp = "B+"},
            new BldGrp() {bldgrp = "B-"},
            new BldGrp() {bldgrp = "AB+"},
            new BldGrp() {bldgrp = "AB-"},
            new BldGrp() {bldgrp = "O+"},
            new BldGrp() {bldgrp = "O-"}
       };

        //0 : Resigned / 1 : Working/ 2 : Terminated
        public List<CurrStat> lstCurrStat = new List<CurrStat>()
        {
            new CurrStat() {currid = 1,currstat = "Working"},
            new CurrStat() {currid = 0,currstat = "Resigned"},
            new CurrStat() {currid = 2,currstat = "Terminated"}                 
       };

        public List<AcctTyp> lstAcctTyp = new List<AcctTyp>()
        {            
            new AcctTyp() {accttype = "Savings"},
            new AcctTyp() {accttype = "Curent"},
            new AcctTyp() {accttype = "Loan"},
            new AcctTyp() {accttype = "Other"}
       };

        public List<Occup> lstOccup = new List<Occup>()
        {
            new Occup() {occp = "None"},
            new Occup() {occp = "Student"},
            new Occup() {occp = "Working"}
            
       };

        public List<ResType> lstRestype = new List<ResType>()
        {
            new ResType() {restype = "Own Property"},
            new ResType() {restype = "Rent"}
       };

    }
}
