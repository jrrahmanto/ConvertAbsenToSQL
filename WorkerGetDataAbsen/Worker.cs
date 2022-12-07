using Microsoft.EntityFrameworkCore;
using System.Data.OleDb;
using System.Security.Cryptography;
using static WorkerGetDataAbsen.Model;

namespace WorkerGetDataAbsen
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                try
                {
                    var dbcontext = new Data();
                    var master_absen = (from p in dbcontext.MMesinAbsen
                                        select p).Where(x => x.isdelete == 0).ToList();
                    var master_karyawan = (from p in dbcontext.MEmployee
                                           select p).Where(x => x.isdelete == 0 && x.emp_aktif == "t").ToList();
                    var jam = DateTime.Now.Hour;
                    if (jam == 2)
                    {
                        foreach (var item in master_karyawan)
                        {
                            var data_absen = (from p in dbcontext.TAbsensi
                                              select p).Where(x => x.NIP == item.nip.ToString() && x.update_date == DateTime.Now.Date).FirstOrDefault();
                            if (data_absen == null)
                            {
                                var checkAbsenKhusus = (from a in dbcontext.TAbsenKhusus where a.nip == item.nip && (a.periode_start <= DateTime.Now.Date && DateTime.Now.Date <= a.periode_end) && a.isdelete == 0 select a).ToList();
                                var checkAbsenKhususAll = (from a in dbcontext.TAbsenKhusus where a.nip == 999 && (a.periode_start <= DateTime.Now.Date && DateTime.Now.Date <= a.periode_end) && a.isdelete == 0 select a).ToList();
                                if (checkAbsenKhusus.Count != 0 || checkAbsenKhususAll.Count !=0)
                                {
                                    var absen = new TAbsensi();
                                    absen.NIP = item.nip.ToString();
                                    absen.update_date = DateTime.Now.Date;
                                    absen.Jam_Masuk = null;
                                    absen.Jam_Keluar = null;
                                    absen.Lembur = null;
                                    absen.Nominal_Lembur = 0;
                                    absen.Hitung_Lembur = false;
                                    if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday || DateTime.Now.DayOfWeek == DayOfWeek.Saturday)
                                    {
                                        absen.Status = DateTime.Now.DayOfWeek.ToString();
                                    }
                                    else
                                    {
                                        absen.Status = "masuk";
                                    }
                                    if (checkAbsenKhusus.Count != 0)
                                    {
                                        absen.keterangan = checkAbsenKhusus[0].keterangan;
                                    }
                                    else
                                    {
                                        absen.keterangan = checkAbsenKhususAll[0].keterangan;
                                    }
                                    dbcontext.Add(absen);
                                    dbcontext.SaveChanges();

                                }
                                else
                                {
                                    var absen = new TAbsensi();
                                    absen.NIP = item.nip.ToString();
                                    absen.update_date = DateTime.Now.Date;
                                    absen.Jam_Masuk = null;
                                    absen.Jam_Keluar = null;
                                    absen.Lembur = null;
                                    absen.Nominal_Lembur = 0;
                                    absen.Hitung_Lembur = false;
                                    if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday || DateTime.Now.DayOfWeek == DayOfWeek.Saturday)
                                    {
                                        absen.Status = DateTime.Now.DayOfWeek.ToString();
                                    }
                                    else
                                    {
                                        absen.Status = "tidak masuk";
                                    }

                                    dbcontext.Add(absen);
                                    dbcontext.SaveChanges();
                                }
                            }
                        }
                    }
                    else
                    {
                        var datamesinIN = new List<dataMesin>();
                        var datamesinOUT = new List<dataMesin>();
                        using (var conection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;" + "data source=C:\\Program Files (x86)\\Att\\att2000.mdb;"))
                        //using (var conection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;" + "data source=D:\\att2000.mdb;"))

                        {
                            conection.Open();
                            var query = "Select TOP 500 CHECKTIME, USERID From CHECKINOUT ORDER BY CHECKTIME DESC";
                            var command = new OleDbCommand(query, conection);
                            var reader = command.ExecuteReader();
                            while (reader.Read())
                                //update data jam keluar

                                //if (Convert.ToDateTime(reader[0].ToString()).Date == DateTime.Now.Date && Convert.ToDateTime(reader[0].ToString()).Hour < 12)
                                if (Convert.ToDateTime(reader[0].ToString()).Hour < 12)
                                {
                                    datamesinIN.Add(new dataMesin
                                    {
                                        date = Convert.ToDateTime(reader[0].ToString()),
                                        id_mechine = Convert.ToInt32(reader[1].ToString())
                                    });
                                }
                                //else if (Convert.ToDateTime(reader[0].ToString()).Date == DateTime.Now.Date.AddDays(-1) && Convert.ToDateTime(reader[0].ToString()).Hour > 12)
                                else if (Convert.ToDateTime(reader[0].ToString()).Hour > 12)
                                {
                                    datamesinOUT.Add(new dataMesin
                                    {
                                        date = Convert.ToDateTime(reader[0].ToString()),
                                        id_mechine = Convert.ToInt32(reader[1].ToString())
                                    });
                                }
                        }
                        var result_datamesinIN = datamesinIN.GroupBy(test => test.id_mechine)
                                               .Select(grp => grp.First())
                                               .ToList();

                        var resultIN = (from a in result_datamesinIN
                                        join b in master_absen on a.id_mechine equals b.id_mechine
                                        select new { b.NIP, a.date }).ToList();

                        var result_datamesinOUT = datamesinOUT.GroupBy(test => test.id_mechine)
                                               .Select(grp => grp.First())
                                               .ToList();

                        var resultOUT = (from a in result_datamesinOUT
                                         join b in master_absen on a.id_mechine equals b.id_mechine
                                         select new { b.NIP, a.date }).ToList();

                        foreach (var item in resultIN)
                        {
                            var checkDataAbsen = (from p in dbcontext.TAbsensi select new { p.id, p.NIP, p.update_date }).Where(x => x.NIP == item.NIP && item.date.Date == x.update_date.Date).ToList();

                            if (checkDataAbsen.Count != 0)
                            {
                                TimeSpan start = new TimeSpan(08, 1, 0);

                                var absen = new TAbsensi();
                                absen.id = checkDataAbsen[0].id;
                                absen.NIP = item.NIP;
                                absen.update_date = item.date.Date;
                                absen.Jam_Masuk = item.date;
                                absen.Lembur = null;
                                absen.Nominal_Lembur = 0;
                                absen.Hitung_Lembur = false;
                                if ((item.date.TimeOfDay > start))
                                {
                                    absen.Status = "terlambat";
                                }
                                else
                                {
                                    absen.Status = "masuk";
                                }

                                dbcontext.TAbsensi.Update(absen);
                                dbcontext.SaveChanges();

                            }
                        }
                        var context = new Data();
                        foreach (var item in resultOUT)
                        {
                            var checkDataAbsen = (from p in dbcontext.TAbsensi select new { p.id, p.NIP, p.update_date, p.Jam_Masuk }).Where(x => x.NIP == item.NIP && item.date.Date == x.update_date.Date).ToList();
                            if (checkDataAbsen.Count != 0)
                            {
                                TimeSpan lembur = (item.date - Convert.ToDateTime(DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd 17:00:00")));
                                if (lembur.TotalHours < 1)
                                {
                                    lembur = TimeSpan.Parse("00:00:00");
                                }
                                var absen = new TAbsensi();
                                absen.id = checkDataAbsen[0].id;
                                absen.NIP = item.NIP;
                                absen.Jam_Masuk = checkDataAbsen[0].Jam_Masuk;
                                absen.update_date = item.date.Date;
                                absen.Jam_Keluar = item.date;
                                absen.Lembur = lembur;
                                absen.Nominal_Lembur = 0;
                                absen.Hitung_Lembur = false;
                                absen.Status = "masuk";
                                context.TAbsensi.Update(absen);
                                context.SaveChanges();
                            }
                        }
                    }
                }
                catch (Exception x)
                {

                    throw;
                }
                await Task.Delay(600000, stoppingToken);
            }
        }
    }
}