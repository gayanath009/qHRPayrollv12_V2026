using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace qHRM.Areas.Notify.Models
{
    public partial class NotificationContext : DbContext
    {
        public NotificationContext()
        {
        }

        public NotificationContext(DbContextOptions<NotificationContext> options)
            : base(options)
        {
        }

        public virtual DbSet<NtfDataSource> NtfDataSource { get; set; }
        public virtual DbSet<NtfList> NtfList { get; set; }
        public virtual DbSet<NtfListTypeRef> NtfListTypeRef { get; set; }
        public virtual DbSet<NtfModule> NtfModule { get; set; }
        public virtual DbSet<NtfMsgTmpl> NtfMsgTmpl { get; set; }
        public virtual DbSet<NtfMsgTmplParamaters> NtfMsgTmplParamaters { get; set; }
        public virtual DbSet<NtfNotification> NtfNotification { get; set; }
        public virtual DbSet<NtfProcess> NtfProcess { get; set; }
        public virtual DbSet<NtfRecipients> NtfRecipients { get; set; }
        public virtual DbSet<NtfScheduleDetail> NtfScheduleDetail { get; set; }
        public virtual DbSet<NtfScheduleHeader> NtfScheduleHeader { get; set; }
        public virtual DbSet<NtfScheduleLog> NtfScheduleLog { get; set; }
        public virtual DbSet<NtfSendMode> NtfSendMode { get; set; }
        public virtual DbSet<NtfType> NtfType { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //if (!optionsBuilder.IsConfigured)
            //{
            //    optionsBuilder.UseSqlServer("Server=GAYANATH-NB;Database=Notification;Trusted_Connection=True;");
            //}
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NtfDataSource>(entity =>
            {
                entity.HasKey(e => e.DsId);

                entity.ToTable("NTF_DataSource");

                entity.Property(e => e.DsId).HasColumnName("Ds_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.DsCode)
                    .HasColumnName("Ds_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.DsDesc)
                    .HasColumnName("Ds_Desc")
                    .HasMaxLength(100);

                entity.Property(e => e.NtfId).HasColumnName("Ntf_Id");

                entity.Property(e => e.PriorDays).HasColumnName("Prior_Days");

                entity.Property(e => e.SysField)
                    .HasColumnName("sysField")
                    .HasMaxLength(100);

                entity.Property(e => e.SysTbl)
                    .HasColumnName("sysTbl")
                    .HasMaxLength(100);

                entity.HasOne(d => d.Ntf)
                    .WithMany(p => p.NtfDataSource)
                    .HasForeignKey(d => d.NtfId)
                    .HasConstraintName("FK_NTF_DataSource_NTF_List");
            });

            modelBuilder.Entity<NtfList>(entity =>
            {
                entity.HasKey(e => e.NtfId);

                entity.ToTable("NTF_List");

                entity.Property(e => e.NtfId).HasColumnName("Ntf_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.ModId).HasColumnName("Mod_Id");

                entity.Property(e => e.NtfCode)
                    .HasColumnName("Ntf_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.NtfDesc)
                    .HasColumnName("Ntf_Desc")
                    .HasMaxLength(100);

                entity.Property(e => e.NtfName)
                    .HasColumnName("Ntf_Name")
                    .HasMaxLength(50);

                entity.HasOne(d => d.Mod)
                    .WithMany(p => p.NtfList)
                    .HasForeignKey(d => d.ModId)
                    .HasConstraintName("FK_NTF_List_NTF_Module");
            });

            modelBuilder.Entity<NtfListTypeRef>(entity =>
            {
                entity.HasKey(e => e.NtfTypRefId);

                entity.ToTable("NTF_List_Type_Ref");

                entity.Property(e => e.NtfTypRefId).HasColumnName("Ntf_Typ_Ref_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.IsActive).HasColumnName("Is_Active");

                entity.Property(e => e.NsmId).HasColumnName("Nsm_Id");

                entity.Property(e => e.NtfId).HasColumnName("Ntf_Id");

                entity.Property(e => e.TmplId).HasColumnName("Tmpl_Id");

                entity.Property(e => e.TypId).HasColumnName("Typ_Id");

                entity.HasOne(d => d.Nsm)
                    .WithMany(p => p.NtfListTypeRef)
                    .HasForeignKey(d => d.NsmId)
                    .HasConstraintName("FK_NTF_List_Type_Ref_NTF_Send_Mode");

                entity.HasOne(d => d.Ntf)
                    .WithMany(p => p.NtfListTypeRef)
                    .HasForeignKey(d => d.NtfId)
                    .HasConstraintName("FK_NTF_List_Type_Ref_NTF_List");

                entity.HasOne(d => d.Tmpl)
                    .WithMany(p => p.NtfListTypeRef)
                    .HasForeignKey(d => d.TmplId)
                    .HasConstraintName("FK_NTF_List_Type_Ref_NTF_Msg_Tmpl");

                entity.HasOne(d => d.Typ)
                    .WithMany(p => p.NtfListTypeRef)
                    .HasForeignKey(d => d.TypId)
                    .HasConstraintName("FK_NTF_List_Type_Ref_NTF_Type");
            });

            modelBuilder.Entity<NtfModule>(entity =>
            {
                entity.HasKey(e => e.ModId);

                entity.ToTable("NTF_Module");

                entity.Property(e => e.ModId).HasColumnName("Mod_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.ModCode)
                    .HasColumnName("Mod_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.ModDesc)
                    .HasColumnName("Mod_Desc")
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<NtfMsgTmpl>(entity =>
            {
                entity.HasKey(e => e.TmplId);

                entity.ToTable("NTF_Msg_Tmpl");

                entity.Property(e => e.TmplId).HasColumnName("Tmpl_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.MessageBody).HasColumnName("Message_Body");

                entity.Property(e => e.MessageSubject)
                    .HasColumnName("Message_Subject")
                    .HasMaxLength(200);

                entity.Property(e => e.TmplCode)
                    .HasColumnName("Tmpl_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.TmplDesc)
                    .HasColumnName("Tmpl_Desc")
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<NtfMsgTmplParamaters>(entity =>
            {
                entity.HasKey(e => e.ParamId);

                entity.ToTable("NTF_Msg_Tmpl_Paramaters");

                entity.Property(e => e.ParamId).HasColumnName("Param_id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.ParamType)
                    .HasColumnName("Param_Type")
                    .HasMaxLength(50);

                entity.Property(e => e.ParamValue)
                    .HasColumnName("Param_Value")
                    .HasMaxLength(50);

                entity.Property(e => e.TmplId).HasColumnName("Tmpl_Id");

                entity.HasOne(d => d.Tmpl)
                    .WithMany(p => p.NtfMsgTmplParamaters)
                    .HasForeignKey(d => d.TmplId)
                    .HasConstraintName("FK_NTF_Msg_Tmpl_Paramaters_NTF_Msg_Tmpl");
            });

            modelBuilder.Entity<NtfNotification>(entity =>
            {
                entity.HasKey(e => e.MsgId);

                entity.ToTable("NTF_Notification");

                entity.Property(e => e.MsgId).HasColumnName("Msg_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.IsActionTaken).HasColumnName("Is_Action_Taken");

                entity.Property(e => e.IsActionTakenBy).HasColumnName("Is_Action_TakenBy");

                entity.Property(e => e.NtfTypRefId).HasColumnName("Ntf_Typ_Ref_Id");

                entity.Property(e => e.PushReadStat).HasColumnName("Push_Read_Stat");

                entity.Property(e => e.IsDelivered).HasColumnName("Is_Delivered");
                
                entity.Property(e => e.EftMnth).HasColumnName("Eft_Month");

                entity.Property(e => e.EftYr).HasColumnName("Eft_Year");

                entity.Property(e => e.ReqId).HasColumnName("Req_Id");

                entity.Property(e => e.Subject).HasMaxLength(100);

                entity.HasOne(d => d.NtfTypRef)
                    .WithMany(p => p.NtfNotification)
                    .HasForeignKey(d => d.NtfTypRefId)
                    .HasConstraintName("FK_NTF_Notification_NTF_List_Type_Ref");
            });

            modelBuilder.Entity<NtfProcess>(entity =>
            {
                entity.HasKey(e => e.ProId);

                entity.ToTable("NTF_Process");

                entity.Property(e => e.ProId).HasColumnName("Pro_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.ProDesc)
                    .HasColumnName("Pro_Desc")
                    .HasMaxLength(100);

                entity.Property(e => e.ProName)
                    .HasColumnName("Pro_Name")
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<NtfRecipients>(entity =>
            {
                entity.HasKey(e => e.NtfRcptId);

                entity.ToTable("NTF_Recipients");

                entity.Property(e => e.NtfRcptId).HasColumnName("Ntf_Rcpt_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.NtfTypRefId).HasColumnName("Ntf_Typ_Ref_Id");

                entity.Property(e => e.Recipients).HasMaxLength(100);

                entity.HasOne(d => d.NtfTypRef)
                    .WithMany(p => p.NtfRecipients)
                    .HasForeignKey(d => d.NtfTypRefId)
                    .HasConstraintName("FK_NTF_Recipients_NTF_List_Type_Ref");
            });

            modelBuilder.Entity<NtfScheduleDetail>(entity =>
            {
                entity.HasKey(e => e.AutoId);
                //entity.HasNoKey();

                entity.ToTable("NTF_Schedule_Detail");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.NtfTypRefId).HasColumnName("Ntf_Typ_Ref_Id");

                entity.Property(e => e.SchId).HasColumnName("Sch_Id");

                entity.HasOne(d => d.NtfTypRef)
                    .WithMany(p => p.NtfScheduleDetail)
                    .HasForeignKey(d => d.NtfTypRefId)
                    .HasConstraintName("FK_NTF_Schedule_Detail_NTF_List_Type_Ref");

                entity.HasOne(d => d.Sch)
                    .WithMany(p => p.NtfScheduleDetail)
                    .HasForeignKey(d => d.SchId)
                    .HasConstraintName("FK_NTF_Schedule_Detail_NTF_Schedule_Header");
            });

            modelBuilder.Entity<NtfScheduleHeader>(entity =>
            {
                entity.HasKey(e => e.SchId);

                entity.ToTable("NTF_Schedule_Header");

                entity.Property(e => e.SchId).HasColumnName("Sch_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.ProId).HasColumnName("Pro_Id");

                entity.Property(e => e.SchDaysOfMonth)
                    .HasColumnName("Sch_Days_Of_Month")
                    .HasMaxLength(50);

                entity.Property(e => e.SchDaysOfWeek)
                    .HasColumnName("Sch_Days_Of_Week")
                    .HasMaxLength(100);

                entity.Property(e => e.SchDesc)
                    .HasColumnName("Sch_Desc")
                    .HasMaxLength(300);

                entity.Property(e => e.SchEndDate)
                    .HasColumnName("Sch_End_Date")
                    .HasColumnType("date");

                entity.Property(e => e.SchFrequency)
                    .HasColumnName("Sch_Frequency")
                    .HasMaxLength(3);

                entity.Property(e => e.SchInfinity).HasColumnName("Sch_Infinity");

                entity.Property(e => e.SchStartDate)
                    .HasColumnName("Sch_Start_Date")
                    .HasColumnType("date");

                entity.Property(e => e.SchStartTime).HasColumnName("Sch_Start_Time");

                entity.HasOne(d => d.Pro)
                    .WithMany(p => p.NtfScheduleHeader)
                    .HasForeignKey(d => d.ProId)
                    .HasConstraintName("FK_NTF_Schedule_Header_NTF_Process");
            });

            modelBuilder.Entity<NtfScheduleLog>(entity =>
            {
                //entity.HasNoKey();
                entity.HasKey(e => e.SchId);

                entity.ToTable("NTF_Schedule_Log");

                entity.Property(e => e.SchErrorLogfileRef)
                    .HasColumnName("Sch_Error_Logfile_Ref")
                    .HasMaxLength(50);

                entity.Property(e => e.SchId)
                    .HasColumnName("Sch_Id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.SchLogfileRef)
                    .HasColumnName("Sch_Logfile_Ref")
                    .HasMaxLength(100);

                entity.Property(e => e.SchProcessEnd)
                    .HasColumnName("Sch_Process_End")
                    .HasColumnType("datetime");

                entity.Property(e => e.SchProcessStart)
                    .HasColumnName("Sch_Process_Start")
                    .HasColumnType("datetime");

                entity.Property(e => e.SchStat).HasColumnName("Sch_Stat");

                entity.HasOne(d => d.Sch)
                    .WithMany(p => p.NtfScheduleLog)
                    .HasForeignKey(d => d.SchId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_NTF_Schedule_Log_NTF_Schedule_Header");
            });

            modelBuilder.Entity<NtfSendMode>(entity =>
            {
                entity.HasKey(e => e.NsmId);

                entity.ToTable("NTF_Send_Mode");

                entity.Property(e => e.NsmId).HasColumnName("Nsm_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.NsmCode)
                    .HasColumnName("Nsm_Code")
                    .HasMaxLength(20);

                entity.Property(e => e.NsmDesc)
                    .HasColumnName("Nsm_Desc")
                    .HasMaxLength(100);
            });

            modelBuilder.Entity<NtfType>(entity =>
            {
                entity.HasKey(e => e.TypId);

                entity.ToTable("NTF_Type");

                entity.Property(e => e.TypId).HasColumnName("Typ_Id");

                entity.Property(e => e.CrtDateTime)
                    .HasColumnName("Crt_DateTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.RecStat).HasColumnName("Rec_Stat");

                entity.Property(e => e.TypCode)
                    .HasColumnName("Typ_Code")
                    .HasMaxLength(10);

                entity.Property(e => e.TypDesc)
                    .HasColumnName("Typ_Desc")
                    .HasMaxLength(100);

                entity.Property(e => e.TypName)
                    .HasColumnName("Typ_Name")
                    .HasMaxLength(50);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
