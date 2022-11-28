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
                    var master_karyawan = (from p in dbcontext.MKaryawan
                                           select p).Where(x => x.isdelete == 0).ToList();
                    var jam = DateTime.Now.Hour;
                    if (jam == 2)
                    {
                        foreach (var item in master_karyawan)
                        {
                            var absen = new TAbsensi();
                            absen.NIP = item.NIP;
                            absen.update_date = DateTime.Now;
                            absen.Jam_Masuk = null;
                            absen.Jam_Keluar = null;
                            absen.Lembur = null;
                            absen.Nominal_Lembur = 0;
                            absen.Hitung_Lembur = false;
                            absen.Status = "tidak masuk";
                            dbcontext.Add(absen);
                            dbcontext.SaveChanges();
                        }
                    }
                    var datamesinIN = new List<dataMesin>();
                    var datamesinOUT = new List<dataMesin>();
                    using (var conection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;" + "data source=C:\\Program Files (x86)\\Att\\att2000.mdb;"))
                    //using (var conection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;" + "data source=D:\\att2000.mdb;"))

                    {
                        conection.Open();
                        var query = "Select TOP 300 CHECKTIME, USERID From CHECKINOUT ORDER BY CHECKTIME DESC";
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
                            Console.WriteLine(item.NIP + " - " + item.date);
                            var absen = new TAbsensi();
                            absen.id = checkDataAbsen[0].id;
                            absen.NIP = item.NIP;
                            absen.update_date = item.date.Date;
                            absen.Jam_Masuk = item.date;
                            absen.Lembur = null;
                            absen.Nominal_Lembur = 0;
                            absen.Hitung_Lembur = false;
                            absen.Status = "masuk";
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
                catch (Exception x)
                {

                    throw;
                }
                await Task.Delay(3600000, stoppingToken);
            }
        }
    }
}