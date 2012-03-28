﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using DSWrapper;
using System.Threading;

namespace HomeMediaCenter
{
    internal class DirectShowEncoder : EncoderBuilder
    {
        private enum DSCodec { MPEG2_PS, MPEG2LAYER1_PS, MPEG2_TS, MPEG2LAYER1_TS, WEBM, WEBM_TS, WMV2, WMV3A2 }

        private Dictionary<string, string> parameters;
        private DSEncoder encoder;
        private DSCodec codec;

        public static DirectShowEncoder TryCreate(Dictionary<string, string> parameters)
        {
            DSCodec codecEnum;
            if (Enum.TryParse<DSCodec>(parameters["codec"], true, out codecEnum))
            {
                DirectShowEncoder encoder = new DirectShowEncoder();
                encoder.codec = codecEnum;
                encoder.parameters = parameters;
                return encoder;
            }

            return null;
        }

        public override string GetMime()
        {
            bool onlyAudio = this.parameters.ContainsKey("video") && this.parameters["video"] == "0";
            switch (this.codec)
            {
                case DSCodec.MPEG2_PS: return onlyAudio ? "audio/mpeg" : "video/mpeg";
                case DSCodec.MPEG2_TS: goto case DSCodec.MPEG2_PS;
                case DSCodec.MPEG2LAYER1_PS: goto case DSCodec.MPEG2_PS;
                case DSCodec.MPEG2LAYER1_TS: goto case DSCodec.MPEG2_PS;
                case DSCodec.WEBM: return onlyAudio ? "audio/webm" : "video/webm";
                case DSCodec.WEBM_TS: goto case DSCodec.WEBM;
                case DSCodec.WMV2: return onlyAudio ? "audio/x-ms-wma" : "video/x-ms-wmv";
                case DSCodec.WMV3A2: goto case DSCodec.WMV2;
            }

            return string.Empty;
        }

        public override void StopEncode()
        {
            DSEncoder enc = this.encoder;
            if (enc != null)
                enc.StopEncode();
        }

        public override void StartEncode(Stream output)
        {
            using (DSEncoder enc = new DSEncoder())
            {
                this.encoder = enc;

                if (parameters["source"].ToLower() == "desktop")
                {
                    //Zdroj je plocha PC
                    if (parameters.ContainsKey("desktopfps"))
                        enc.SetInput(InputType.Desktop(uint.Parse(parameters["desktopfps"])));
                    else
                        enc.SetInput(InputType.Desktop(10));
                }
                else if (parameters["source"].ToLower().StartsWith("webcam"))
                {
                    //Zdroj je webkamera
                    string[] webcamParam = parameters["source"].Split(new char[] { '_' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (webcamParam.Length == 1)
                        enc.SetInput(InputType.Webcam(WebcamInput.GetWebcamNames().First()));
                    else
                        enc.SetInput(InputType.Webcam(webcamParam[1]));
                }
                else
                {
                    //Zdrj je subor, http link
                    bool reqSeeking = (parameters.ContainsKey("starttime") || parameters.ContainsKey("endtime")) &&
                        this.codec != DSCodec.MPEG2_PS && this.codec != DSCodec.MPEG2_TS &&
                        this.codec != DSCodec.MPEG2LAYER1_PS && this.codec != DSCodec.MPEG2LAYER1_TS;
                    enc.SetInput(parameters["source"], reqSeeking);
                }

                uint setVideo = 1, setAudio = 1;
                if (parameters.ContainsKey("video"))
                    setVideo = uint.Parse(parameters["video"]);
                if (parameters.ContainsKey("audio"))
                    setAudio = uint.Parse(parameters["audio"]);

                //Nastavenie video a zvukovej stopy
                uint actVideo = 1, actAudio = 1;
                foreach (PinInfoItem item in enc.SourcePins)
                {
                    if (item.MediaType == PinMediaType.Video)
                    {
                        if (actVideo == setVideo)
                            item.IsSelected = true;
                        else
                            item.IsSelected = false;
                        actVideo++;
                    }
                    else if (item.MediaType == PinMediaType.Audio)
                    {
                        if (actAudio == setAudio)
                            item.IsSelected = true;
                        else
                            item.IsSelected = false;
                        actAudio++;
                    }
                    else
                    {
                        item.IsSelected = false;
                    }
                }

                //Zistenie sirky a vysky, povodna hodnota ak nezadane
                uint width = 0, height = 0;
                if (parameters.ContainsKey("width"))
                    width = uint.Parse(parameters["width"]);
                if (parameters.ContainsKey("height"))
                    height = uint.Parse(parameters["height"]);

                //Zistenie bitrate pre audio a vide, povodna hodnota ak nezadane
                uint vidBitrate = 0, audBitrate = 0;
                if (parameters.ContainsKey("vidbitrate"))
                    vidBitrate = uint.Parse(parameters["vidbitrate"]);
                if (parameters.ContainsKey("audbitrate"))
                    audBitrate = uint.Parse(parameters["audbitrate"]);

                //Zistenie kvality videa
                uint quality = 50;
                if (parameters.ContainsKey("quality"))
                {
                    quality = uint.Parse(parameters["quality"]);
                    if (quality < 1 || quality > 100)
                        throw new HttpException(400, "Encode quality must be in range 1-100");
                }

                //Zistenie poctu snimok za sekundu
                uint fps = 0;
                if (parameters.ContainsKey("fps"))
                    fps = uint.Parse(parameters["fps"]);

                //Zistenie ci zachovat pomer stran pri zmene rozlisenia
                bool keepAspect = parameters.ContainsKey("keepaspect");

                //Integrovanie titulkov, ak nie je zadany nazov - automaticka detekcia
                //Ak je zadany nazov - nastav prislusny subor
                string subPath = null;
                bool subtitles = parameters.ContainsKey("subtitles");
                if (subtitles)
                {
                    if (parameters["subtitles"] != string.Empty)
                        subPath = parameters["subtitles"];
                }

                ContainerType container = null;
                switch (this.codec)
                {
                    case DSCodec.MPEG2_PS: container = ContainerType.MPEG2_PS(width, height, BitrateMode.CBR, vidBitrate * 1000, quality,
                        fps, ScanType.Interlaced, subtitles, subPath, keepAspect, MpaLayer.Layer2, audBitrate * 1000);
                        break;
                    case DSCodec.MPEG2_TS: container = ContainerType.MPEG2_TS(width, height, BitrateMode.CBR, vidBitrate * 1000, quality,
                        fps, ScanType.Interlaced, subtitles, subPath, keepAspect, MpaLayer.Layer2, audBitrate * 1000);
                        break;
                    case DSCodec.MPEG2LAYER1_PS: container = ContainerType.MPEG2_PS(width, height, BitrateMode.CBR, vidBitrate * 1000, quality,
                        fps, ScanType.Interlaced, subtitles, subPath, keepAspect, MpaLayer.Layer1, audBitrate * 1000);
                        break;
                    case DSCodec.MPEG2LAYER1_TS: container = ContainerType.MPEG2_TS(width, height, BitrateMode.CBR, vidBitrate * 1000, quality,
                        fps, ScanType.Interlaced, subtitles, subPath, keepAspect, MpaLayer.Layer1, audBitrate * 1000);
                        break;
                    case DSCodec.WEBM: container = ContainerType.WEBM(width, height, BitrateMode.CBR, vidBitrate, quality, fps, subtitles, 
                        subPath, keepAspect, audBitrate);
                        break;
                    case DSCodec.WEBM_TS: container = ContainerType.WEBM_TS(width, height, BitrateMode.CBR, vidBitrate, quality, fps, subtitles,
                        subPath, keepAspect, audBitrate);
                        break;
                    case DSCodec.WMV2: container = ContainerType.WMV(width, height, WMVideoSubtype.WMMEDIASUBTYPE_WMV2, vidBitrate * 1000, quality, 
                        fps, subtitles, subPath, audBitrate * 1000);
                        break;
                    case DSCodec.WMV3A2: container = ContainerType.WMV(width, height, WMVideoSubtype.WMMEDIASUBTYPE_WMV3, vidBitrate * 1000, quality,
                        fps, subtitles, subPath, audBitrate * 1000);
                        break;
                }

                //Nastavenie casu zaciatku a konca v sekundach
                long startTime = 0, endTime = 0;
                if (parameters.ContainsKey("starttime"))
                    startTime = (long)(double.Parse(parameters["starttime"], System.Globalization.CultureInfo.InvariantCulture) * 10000000);
                if (parameters.ContainsKey("endtime"))
                    endTime = (long)(double.Parse(parameters["endtime"], System.Globalization.CultureInfo.InvariantCulture) * 10000000);

                if (this.progressChangeDel != null)
                    enc.ProgressChange += new EventHandler<ProgressChangeEventArgs>(enc_ProgressChange);

                //Nastavenie vystupu
                enc.SetOutput(output, container, startTime, endTime);

                for (int i = 0; ; i++)
                {
                    try 
                    { 
                        enc.StartEncode();
                        break;
                    }
                    catch (DSException ex)
                    {
                        //chyba -2147023446 znamena ze webkamera uz je pouzivana, pocka sa dany cas a pokusi sa spustit znova
                        if (ex.Result != -2147023446 || i == 3)
                            throw;
                        else
                            Thread.Sleep(3000);
                    }
                }
            }
        }

        private void enc_ProgressChange(object sender, ProgressChangeEventArgs e)
        {
            this.progressChangeDel(e.Progress);
        }
    }
}