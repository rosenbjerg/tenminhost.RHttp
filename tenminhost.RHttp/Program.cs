﻿using System;
using System.IO;
using RHttpServer;

namespace tenminhost.RHttp
{
    internal class Program
    {
        private static readonly Random Random = new Random();
        private static readonly string Root = Path.GetPathRoot(Path.GetFullPath("Server.exe"));
        private static readonly DriveInfo Di = new DriveInfo(Root);
        private const long FreeLimit = 0x140000000;

        public static void Main(string[] args)
        {
            var server = new TaskBasedHttpServer(5001);
            const string uploadFolder = "./uploads/";

            server.Get("/", (req, res) =>
            {
                res.RenderPage("pages/index.ecs", new RenderParams
                    {
                        {"id", ""},
                        {"canUpload", FreeSpace()}
                    });
            });

            server.Get("/favicon.ico", (req, res) =>
            {
                res.SendFile("pages/favicon.ico");
            });

            server.Get("/:file", (req, res) =>
            {
                var filename = req.Params["file"];
                if (File.Exists(uploadFolder + filename))
                {
                    res.RenderPage("pages/index.ecs", new RenderParams
                    {
                        {"id", filename},
                        {"canUpload", false}
                    });
                }
                else
                {
                    res.Redirect("/");
                }

            });

            server.Get("/:file/download", (req, res) =>
            {
                var filename = uploadFolder + req.Params["file"];
                if (File.Exists(filename))
                    res.Download(filename);
                else
                    res.Redirect("/");
            });

            server.Post("/upload", async (req, res) =>
            {
                if (!FreeSpace())
                {
                    res.SendString("Error, server is full", status: 400);
                    return;
                }
                var fname = "";
                var sa = await req.SaveBodyToFile("./uploads", s =>
                {
                    s = $"{GetRandomString(4)}-{s}";
                    fname = s;
                    return s;
                }, 0x7D000);
                if (!sa) res.SendString("Error occurred while saving", status: 413);
                else res.SendString(fname);
            });

            //server.Options("/upload", (req, res) =>
            //{
            //    res.AddHeader("Access-Control-Allow-Origin", allowedOrigin);
            //    res.AddHeader("Access-Control-Allow-Methods", "POST");
            //    res.AddHeader("Access-Control-Allow-Headers", "Origin, Content-Type, Cache-Control, X-Requested-With");
            //    res.SendString("");
            //});

            var cleaner = new Cleaner("./uploads", 13);
            cleaner.Start();

            server.Start();
        }

        private static bool FreeSpace()
        {
            return Di.AvailableFreeSpace > FreeLimit;
        }

        private static string GetRandomString(int length)
        {
            var chars = new char[length];
            for (var i = 0; i < length; i++)
            {
                var num = Random.Next(0, 26); // Zero to 25
                chars[i] = (char)('a' + num);
            }
            return new string(chars);
        }
    }
}