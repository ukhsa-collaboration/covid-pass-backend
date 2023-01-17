﻿using System.Collections.Generic;
using CovidCertificate.Backend.Models.DataModels;

namespace CovidCertificate.Backend.Services
{
    public static class TransliterationModelList
    {
        public static List<TransliterationsModel> TransliterationList()
        {
            var list = new List<TransliterationsModel>
            {
                new TransliterationsModel("00C0","A grave","A"),
                new TransliterationsModel("00C1",    "A acute" ,"A"),
                new TransliterationsModel("00C2","A circumflex","A"),
                new TransliterationsModel("00C3","A tilde","A"),
                new TransliterationsModel("00C4","A diaeresis","AE"),
                new TransliterationsModel("00C5","A ring above","AA"),
                new TransliterationsModel("00C6","ligature AE","AE"),
                new TransliterationsModel("00C7","C cedilla","C"),
                new TransliterationsModel("00C8","E grave","E"),
                new TransliterationsModel("00C9","E acute","E"),
                new TransliterationsModel("00CA","E circumflex","E"),
                new TransliterationsModel("00CB","E diaeresis","E"),
                new TransliterationsModel("00CC","I grave","I"),
                new TransliterationsModel("00CD","I acute","I"),
                new TransliterationsModel("00CE","I circumflex","I"),
                new TransliterationsModel("00CF","I diaeresis","I"),
                new TransliterationsModel("00D0","Eth","D"),
                new TransliterationsModel("00D1","N tilde","N"),
                new TransliterationsModel("00D2","O grave","O"),
                new TransliterationsModel("00D3","O acute","O"),
                new TransliterationsModel("00D4","O circumflex","O"),
                new TransliterationsModel("00D5","O tilde","O"),
                new TransliterationsModel("00D6","O diaeresis","OE"),
                new TransliterationsModel("00D8","O stroke","OE"),
                new TransliterationsModel("00D9","U grave","U"),
                new TransliterationsModel("00DA","U acute","U"),
                new TransliterationsModel("00DB","U circumflex","U"),
                new TransliterationsModel("00DC","U diaeresis","UE"),
                new TransliterationsModel("00DD","Y acute","Y"),
                new TransliterationsModel("00DE","Thorn (Iceland)","TH"),
                new TransliterationsModel("00DF","double s (Germany)","SS"),
                new TransliterationsModel("0100","A macron","A"),
                new TransliterationsModel("0102","A breve","A"),
                new TransliterationsModel("0104","A ogonek","A"),
                new TransliterationsModel("0106","C acute","C"),
                new TransliterationsModel("0108","C circumflex","C"),
                new TransliterationsModel("010A","C dot above","C"),
                new TransliterationsModel("010C","C caron","C"),
                new TransliterationsModel("010E","D caron","D"),
                new TransliterationsModel("0110","D stroke","D"),
                new TransliterationsModel("0112","E macron","E"),
                new TransliterationsModel("0114","E breve","E"),
                new TransliterationsModel("0116","E dot above","E"),
                new TransliterationsModel("0118","E ogonek","E"),
                new TransliterationsModel("011A","E caron","E"),
                new TransliterationsModel("011C","G circumflex","G"),
                new TransliterationsModel("011E","G breve","G"),
                new TransliterationsModel("0120","G dot above","G"),
                new TransliterationsModel("0122","G cedilla","G"),
                new TransliterationsModel("0124","H circumflex","H"),
                new TransliterationsModel("0126","H stroke","H"),
                new TransliterationsModel("0128","I tilde","I"),
                new TransliterationsModel("012A","I macron","I"),
                new TransliterationsModel("012C","I breve","I"),
                new TransliterationsModel("012E","I ogonek","I"),
                new TransliterationsModel("0130","I dot above","I"),
                new TransliterationsModel("0131","I without dot (Turkey)","I"),
                new TransliterationsModel("0132","ligature IJ","IJ"),
                new TransliterationsModel("0134","J circumflex","J"),
                new TransliterationsModel("0136","K cedilla","K"),
                new TransliterationsModel("0139","L acute","L"),
                new TransliterationsModel("013B","L cedilla","L"),
                new TransliterationsModel("013D","L caron","L"),
                new TransliterationsModel("013F","L middle dot","L"),
                new TransliterationsModel("0141","L stroke","L"),
                new TransliterationsModel("0143","N acute","N"),
                new TransliterationsModel("0145","N cedilla","N"),
                new TransliterationsModel("0147","N caron","N"),
                new TransliterationsModel("014A","Eng","N"),
                new TransliterationsModel("014C","O macron","O"),
                new TransliterationsModel("014E","O breve","O"),
                new TransliterationsModel("0150","O double acute","O"),
                new TransliterationsModel("0152","ligature OE","OE"),
                new TransliterationsModel("0154","R acute","R"),
                new TransliterationsModel("0156","R cedilla","R"),
                new TransliterationsModel("0158","R caron","R"),
                new TransliterationsModel("015A","S acute","SS"),
                new TransliterationsModel("015C","S circumflex","SS"),
                new TransliterationsModel("015E","S cedilla","SS"),
                new TransliterationsModel("0160","S caron","SS"),
                new TransliterationsModel("0162","T cedilla","TH"),
                new TransliterationsModel("0164","T caron","TH"),
                new TransliterationsModel("0166","T stroke","TH"),
                new TransliterationsModel("0168","U tilde","U"),
                new TransliterationsModel("016A","U macron","U"),
                new TransliterationsModel("016C","U breve","U"),
                new TransliterationsModel("016E","U ring above","U"),
                new TransliterationsModel("0170","U double acute","U"),
                new TransliterationsModel("0172","I ogonek","U"),
                new TransliterationsModel("0174","W circumflex","W"),
                new TransliterationsModel("0176","Y circumflex","Y"),
                new TransliterationsModel("0178","Y cdiaeresis","Y"),
                new TransliterationsModel("1079","Z acute","Z"),
                new TransliterationsModel("017B","Z dot above","Z"),
                new TransliterationsModel("017D","Z caron","Z")
            };

            return list;
        }
    }
}
