using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Integrations.Models;
using qHRM.Areas.Leave.Models;


namespace qHRM.Models
{
    public partial class QCSLEAVEContext : DbContext
    {
        public QCSLEAVEContext()
        {
        }

        public QCSLEAVEContext(DbContextOptions<QCSLEAVEContext> options)
            : base(options)
        {
        }

        public virtual DbSet<LvAll> LvAll { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=DB02\\DB02SQL03;database=QCSLEAVE;uid=QHRMAdmin;password=qcs123; Trusted_Connection=False;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LvAll>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("LV_All");

                entity.Property(e => e.ContactNoOnLeave)
                    .HasColumnName("ContactNo_OnLeave")
                    .HasMaxLength(50)
                    .IsFixedLength();

                entity.Property(e => e.Department)
                    .HasMaxLength(50)
                    .IsFixedLength();

                entity.Property(e => e.DutyHandedOver)
                    .HasColumnName("Duty_Handed_Over")
                    .HasMaxLength(50)
                    .IsFixedLength();

                entity.Property(e => e.Email)
                    .HasMaxLength(50)
                    .IsFixedLength();

                entity.Property(e => e.EmpName)
                    .HasMaxLength(100)
                    .IsFixedLength();

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.LvDays).HasColumnName("LV_Days");

                entity.Property(e => e.LvEndDate)
                    .HasColumnName("LV_End_Date")
                    .HasColumnType("datetime");

                entity.Property(e => e.LvEndDateActual)
                    .HasColumnName("LV_End_Date_Actual")
                    .HasColumnType("datetime");

                entity.Property(e => e.LvInitiatedDate)
                    .HasColumnName("LV_Initiated_Date")
                    .HasColumnType("datetime");

                entity.Property(e => e.LvMonth).HasColumnName("LV_Month");

                entity.Property(e => e.LvStartDate)
                    .HasColumnName("LV_Start_Date")
                    .HasColumnType("datetime");

                entity.Property(e => e.LvYear).HasColumnName("LV_Year");

                entity.Property(e => e.Lvtype)
                    .HasColumnName("LVType")
                    .HasMaxLength(50)
                    .IsFixedLength();

                entity.Property(e => e.LvtypeSub)
                    .HasColumnName("LVType_Sub")
                    .HasMaxLength(50)
                    .IsFixedLength();

                entity.Property(e => e.LvtypeSubPay)
                    .HasColumnName("LVType_Sub_Pay")
                    .HasMaxLength(10)
                    .IsFixedLength();

                entity.Property(e => e.Qid)
                    .HasColumnName("QID")
                    .HasMaxLength(15)
                    .IsFixedLength();

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .IsFixedLength();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
