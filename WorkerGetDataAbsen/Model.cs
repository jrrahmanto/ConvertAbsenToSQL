using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerGetDataAbsen
{
    public class Model
    {
        public class MMesinAbsen
        {
            [Key]
            public int id { get; set; }
            public int id_mechine { get; set; }
            public string badge_number { get; set; }
            public string NIP { get; set; }
            public int isdelete { get; set; }
        }

        public class MEmployee
        {
            [Key]
            public long id { get; set; }
            public int nip { get; set; }
            public int isdelete { get; set; }
            public string emp_aktif { get; set; }
        }
        public class TAbsensi
        {
            [Key]
            public int id { get; set; }
            public string NIP { get; set; }
            public DateTime? Jam_Masuk { get; set; }
            public DateTime? Jam_Keluar { get; set; }
            public string Status { get; set; }
            public TimeSpan? Lembur { get; set; }
            public Decimal Nominal_Lembur { get; set; }
            public bool? Hitung_Lembur { get; set; }
            public DateTime update_date { get; set; }
            public string? keterangan { get; set; }
        }
        public class dataMesin
        {
            public int id_mechine { get; set; }
            public DateTime date { get; set; }
        }
        public class TAbsenKhusus
        {
            [Key]
            public int id { get; set; }
            public int nip { get; set; }
            public DateTime periode_start { get; set; }
            public DateTime periode_end { get; set; }
            public string keterangan { get; set; }
            public int isdelete { get; set; }
        }
        public class MHariLibur
        {
            [Key]
            public int id { get; set; }
            public DateTime tanggal { get; set; }
            public string keterangan { get; set; }
            public int isdelete { get; set; }
        }
    }
}
