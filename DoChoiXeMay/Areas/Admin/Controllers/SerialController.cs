﻿using Antlr.Runtime.Misc;
using DoChoiXeMay.Areas.Admin.Data;
using DoChoiXeMay.Filters;
using DoChoiXeMay.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Xml.Linq;

namespace DoChoiXeMay.Areas.Admin.Controllers
{
    [Protect]
    public class SerialController : Controller
    {
        // GET: Admin/Serial
        Model1 dbc = new Model1();
        string DBname = ConfigurationManager.AppSettings["DBname"];
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult ListSerialChuaIn()
        {
            ViewBag.TotalSerialSPchuaIn = dbc.Ser_sp.Where(kh=>kh.DaIn==false).Count();
            ViewBag.TotalSerialBoXchuaIn = dbc.Ser_box.Where(kh => kh.DaIn == false).Count();
            return View();
        }
        public ActionResult AddNewSerial()
        {
            ViewBag.IDMF = new SelectList(dbc.Manufacturers.Where(kh => kh.Sudung == true), "Id", "Name",5);
            ViewBag.IdColor = new SelectList(dbc.Colors.ToList(), "Id", "TenColor");
            ViewBag.IdSize = new SelectList(dbc.Sizes.ToList(), "Id", "TenSize");
            ViewBag.Idver = new SelectList(dbc.Versions.ToList(), "Id", "VerName");
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult AddNewSerial(Ser_sp SerSP,int SoSerialN, string soloName, string soloSTT)
        {
            ViewBag.IDMF = new SelectList(dbc.Manufacturers.Where(kh => kh.Sudung == true), "Id", "Name", 5);
            ViewBag.IdColor = new SelectList(dbc.Colors.ToList(), "Id", "TenColor");
            ViewBag.IdSize = new SelectList(dbc.Sizes.ToList(), "Id", "TenSize");
            ViewBag.Idver = new SelectList(dbc.Versions.ToList(), "Id", "VerName");
            string solosp = soloName + soloSTT;
            string solobox = "BXX" + soloSTT;
            try
            {
                //tạo S/N SP
                for (int i = 0; i < SoSerialN; i++)
                {
                    SerSP.LoSanXuat = solosp;
                    string SN = Utils.XString.MakeAotuSN(10) + SerSP.LoSanXuat;
                    var kt = dbc.Ser_sp.Where(kh => kh.SerialSP.Contains(SN)).ToList();
                    if (kt.Count() > 0)
                    {
                        //Nếu S/N bị trùng, random 50 lần
                        for (int j = 0; j < 50; j++) {
                            SN= Utils.XString.MakeAotuSN(10) + SerSP.LoSanXuat;
                            var ktt = dbc.Ser_sp.Where(kh => kh.SerialSP.Contains(SN)).ToList();
                            if (ktt.Count() == 0)
                            {
                                SerSP.SerialSP = SN;
                                SerSP.QRcode = new Data.SerialData().getQRcode(SN);
                                break;
                            }
                        }
                    }
                    else
                    {
                        SerSP.SerialSP = SN;
                        string sn1 = new Data.SerialData().getImgtext(SN);
                        string qr = new Data.SerialData().getQRcode(SN);
                        SerSP.QRcode = new Data.SerialData().getMergeImg(sn1,qr);
                    }
                    SerSP.Stt = (i + 1).ToString();
                    var kq = new Data.SerialData().InsertSer_sp(SerSP);
                    if (kq==false)
                    {
                        Session["ThongBaoSerialSPchuaIn"] = "Có lỗi Insert Serial SP.";
                        break;
                    }
                }
                //tạo S/N Box
                for (int i = 0; i < SoSerialN; i++)
                {
                    SerSP.LoSanXuat = solobox;
                    string SN = Utils.XString.MakeAotuSN(10) + SerSP.LoSanXuat;
                    var kt = dbc.Ser_box.Where(kh => kh.Serial.Contains(SN)).ToList();
                    if (kt.Count() > 0)
                    {
                        //Nếu S/N bị trùng, random 50 lần
                        for (int j = 0; j < 50; j++)
                        {
                            SN = Utils.XString.MakeAotuSN(10) + SerSP.LoSanXuat;
                            var ktt = dbc.Ser_box.Where(kh => kh.Serial.Contains(SN)).ToList();
                            if (ktt.Count() == 0)
                            {
                                SerSP.SerialSP = SN;
                                SerSP.QRcode = new Data.SerialData().getImgtext(SN) + new Data.SerialData().getQRcode(SN);
                                break;
                            }
                        }
                    }
                    else
                    {
                        SerSP.SerialSP = SN;
                        string sn1 = new Data.SerialData().getImgtext(SN);
                        string qr = new Data.SerialData().getQRcode(SN);
                        SerSP.QRcode = new Data.SerialData().getMergeImg(sn1, qr);
                    }
                    SerSP.Stt = (i + 1).ToString();

                    var kq = new Data.SerialData().InsertSer_Box(SerSP.LoSanXuat,SerSP.SerialSP,SerSP.Sudung,SerSP.Stt,SerSP.Ghichu,SerSP.QRcode);
                    if (kq == false)
                    {
                        Session["ThongBaoSerialBoxchuaIn"] = "Có lỗi Insert Serial Box.";
                        break;
                    }
                }
                Session["ThongBaoSerialSPchuaIn"] = "Tạo mới thành công "+SoSerialN+" Serial SP.";
                Session["ThongBaoSerialBoxchuaIn"] = "Tạo mới thành công " + SoSerialN + " Serial Box.";
            }
            catch (Exception ex) {
                string loi = ex.ToString();
                ModelState.AddModelError("", "Thêm mới Thất Bại !!!!!!!!!!. Có lỗi hệ thống");
                return View();
            }
            return RedirectToAction("ListSerialChuaIn");
        }
        public ActionResult InSNmoitaoBox(int bang=0, int soluong=0, string mayin="")
        {
            try
            {
                if (bang == 2)
                {
                    var SN = dbc.Ser_box.Where(kh => kh.DaIn == false).OrderBy(kh => kh.NgayTao)
                        .Skip(0)
                        .Take(soluong)
                        .ToList();
                    //var countImg = Math.Ceiling(1.0 * SN.Count() / 3);

                    //for(int i = 0; i < countImg; i++)
                    //{
                    //    var SNIMG = SN.Skip(i*3).Take(3).ToList();
                    //    if (SNIMG.Count() == 3)
                    //    {
                    //        Session["Img641"] = SNIMG[0].QRcode;
                    //        Session["Img642"] = SNIMG[1].QRcode;
                    //        Session["Img643"] = SNIMG[2].QRcode;
                    //    }
                    //    else if (SNIMG.Count() == 2)
                    //    {
                    //        Session["Img641"] = SNIMG[0].QRcode;
                    //        Session["Img642"] = SNIMG[1].QRcode;
                    //        Session["Img643"] = "NOSERIALNUMBER";
                    //    }
                    //    else if (SNIMG.Count() == 1)
                    //    {
                    //        Session["Img641"] = SNIMG[0].QRcode;
                    //        Session["Img642"] = "NOSERIALNUMBER";
                    //        Session["Img643"] = "NOSERIALNUMBER";
                    //    }
                    //    PrintDocument pd = new PrintDocument();
                    //    pd.PrintPage += new PrintPageEventHandler(pd_PrintPage);

                    //    pd.PrinterSettings.PrinterName = "Datamax-O'Neil E-4204B Mark III";
                    //    pd.Print();
                    //    Session.Remove("Img641"); Session.Remove("Img642"); Session.Remove("Img643");
                    //}
                    foreach (var sn in SN)
                    {
                        Session["Img64"] = sn.QRcode;
                        PrintDocument pd = new PrintDocument();
                        //pd.DefaultPageSettings.conf.Left = 30;
                        //pd.DefaultPageSettings.Margins.Top = 10;
                        //pd.DefaultPageSettings.Margins.Right = 10;
                        //pd.DefaultPageSettings.Margins.Bottom = 10;
                        //pd.OriginAtMargins = true;
                        pd.PrintPage += new PrintPageEventHandler(pd_PrintPage);
                        pd.PrinterSettings.PrinterName = mayin;
                        pd.Print();
                        Session.Remove("Img64");
                    }
                    return RedirectToAction("ListSerialChuaIn");
                }
                return RedirectToAction("ListSerialChuaIn");
            }
            catch (Exception ex)
            {
                string loi = ex.ToString();
                Session["ThongBaoSerialBoxchuaIn"] = "In thất bại !!!!!!!!!!. Có lỗi hệ thống";
                return RedirectToAction("ListSerialChuaIn");
            }
        }
        void pd_PrintPage(object sender, PrintPageEventArgs ev)
        {
            if(Session["Img64"] != null)
            {
                //string imgscale = new Data.SerialData().getMerge3Img
                //    (Session["Img641"].ToString(), Session["Img642"].ToString(), Session["Img643"].ToString());
                Image img64 = new Data.SerialData().getScaleImg2(Session["Img64"].ToString());
                ev.Graphics.DrawImage(img64, ev.MarginBounds);
            }
        }
        public ActionResult DeleteSerialSP()
        {
            try
            {
                var kt = dbc.Ser_sp.Where(kh=>kh.DaIn==false).ToList();
                if (kt.Count() == 0)
                {
                    Session["ThongBaoSerialSPchuaIn"] = "Không còn S/N SP nào chưa in để Xóa.";
                    return RedirectToAction("ListSerialChuaIn");
                }
                string sql = "DELETE  FROM [" + DBname + "TechZone].[dbo].[Ser_sp] where DaIn=0";
                var XoaChitietXN = dbc.Database.ExecuteSqlCommand(sql);
                Session["ThongBaoSerialSPchuaIn"] = "Xóa tất cả S/N SP thành công.";
                return RedirectToAction("ListSerialChuaIn");
            }
            catch (Exception ex) {
                string loi = ex.ToString();
                Session["ThongBaoSerialSPchuaIn"] = "Có Lỗi hệ thống khi xóa S/N SP !!!";
                return RedirectToAction("ListSerialChuaIn");
            }
        }
        public ActionResult DeleteSerialBox()
        {
            try
            {
                var kt = dbc.Ser_box.Where(kh => kh.DaIn == false).ToList();
                if (kt.Count() == 0)
                {
                    Session["ThongBaoSerialBoxchuaIn"] = "Không còn S/N Box nào chưa in để Xóa.";
                    return RedirectToAction("ListSerialChuaIn");
                }
                string sql = "DELETE  FROM [" + DBname + "TechZone].[dbo].[Ser_box] where DaIn=0";
                var XoaChitietXN = dbc.Database.ExecuteSqlCommand(sql);
                Session["ThongBaoSerialBoxchuaIn"] = "Xóa tất cả S/N Box thành công.";
                return RedirectToAction("ListSerialChuaIn");
            }
            catch (Exception ex)
            {
                string loi = ex.ToString();
                Session["ThongBaoSerialBoxchuaIn"] = "Có Lỗi hệ thống khi xóa S/N Box !!!";
                return RedirectToAction("ListSerialChuaIn");
            }
        }
        public ActionResult GetListSer_SP()
        {
            ViewBag.SerSPchuaIn = dbc.Ser_sp.Where(kh => kh.DaIn == false).OrderBy(kh => kh.NgayTao).ToList();
            return PartialView();
        }
        public ActionResult GetListSer_Box()
        {
            ViewBag.SerBoxchuaIn = dbc.Ser_box.Where(kh => kh.DaIn == false).OrderBy(kh => kh.NgayTao).ToList();
            return PartialView();
        }
    }
}