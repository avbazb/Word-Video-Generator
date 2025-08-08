using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.MediaFoundation;
using FFMpegCore;
using FFMpegCore.Pipes;
using SkiaSharp;
using WordVideoGenerator.Models;
using System.Collections.Generic;
using System.Linq;

namespace WordVideoGenerator.Services
{
    public class VideoService
    {
        private const int VIDEO_WIDTH = 1280;
        private const int VIDEO_HEIGHT = 720;
        private const int FPS = 30;
        private const int REPEAT_COUNT = 30;
        private readonly List<(SKPoint Position, float Rotation)> _wordPositions = new List<(SKPoint, float)>();
        private readonly Random _random = new Random();
        
        public VideoService()
        {
            // 配置FFmpeg二进制文件路径
            ConfigureFFmpegPath();
            
            // 检查FFmpeg是否可用
            if (!FFMpegCore.GlobalFFOptions.GetFFMpegBinaryPath().Any())
            {
                throw new Exception("未检测到FFmpeg。请确保FFmpeg二进制文件已正确配置。");
            }
        }

        private void ConfigureFFmpegPath()
        {
            // 优先使用应用程序目录下的FFmpeg
            var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
            var ffmpegPath = Path.Combine(appDirectory, "ffmpeg", "bin");
            
            // 如果本地FFmpeg目录存在，使用本地版本
            if (Directory.Exists(ffmpegPath) && File.Exists(Path.Combine(ffmpegPath, "ffmpeg.exe")))
            {
                FFMpegCore.GlobalFFOptions.Configure(new FFMpegCore.FFOptions 
                { 
                    BinaryFolder = ffmpegPath,
                    TemporaryFilesFolder = Path.GetTempPath()
                });
                LogService.Instance?.Log($"使用本地FFmpeg: {ffmpegPath}");
            }
            else
            {
                // 如果找不到本地FFmpeg，尝试使用系统PATH中的FFmpeg
                LogService.Instance?.Log("未找到本地FFmpeg，将使用系统PATH中的FFmpeg");
            }
        }

        public async Task<string> DownloadAudioAsync(string word)
        {
            var audioUrl = $"https://dict.youdao.com/dictvoice?audio={word}";
            var outputPath = Path.Combine(Path.GetTempPath(), $"{word}_audio.mp3");
            
            using (var client = new System.Net.Http.HttpClient())
            {
                var bytes = await client.GetByteArrayAsync(audioUrl);
                await File.WriteAllBytesAsync(outputPath, bytes);
            }
            
            return outputPath;
        }

        public async Task GenerateVideoAsync(WordInfo wordInfo)
        {
            _wordPositions.Clear(); // 清除之前的单词位置
            var log = LogService.Instance;
            log.Log($"开始生成视频: {wordInfo.Word}");

            // 1. 处理音频
            using (var reader = new AudioFileReader(wordInfo.AudioPath))
            {
                try
                {
                    // 裁剪静音部分
                    log.Log($"正在处理音频: 裁剪静音部分");
                    var trimmedAudio = TrimSilence(reader);
                    var repeatedAudio = RepeatAudio(trimmedAudio, REPEAT_COUNT);
                    
                    // 将处理后的音频保存为临时文件
                    var tempAudioPath = Path.Combine(Path.GetTempPath(), $"{wordInfo.Word}_processed.wav");
                    log.Log($"正在保存处理后的音频: {tempAudioPath}");
                    using (var writer = new WaveFileWriter(tempAudioPath, repeatedAudio.WaveFormat))
                    {
                        var buffer = new float[repeatedAudio.WaveFormat.SampleRate * repeatedAudio.WaveFormat.Channels];
                        int read;
                        while ((read = repeatedAudio.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            writer.WriteSamples(buffer, 0, read);
                        }
                    }

                    // 2. 生成视频帧
                    var framesList = new List<string>();
                    var framesDir = Path.Combine(Path.GetTempPath(), $"{wordInfo.Word}_frames");
                    Directory.CreateDirectory(framesDir);

                    try
                    {
                        // 计算视频总帧数（基于音频长度）
                        using (var repeatedReader = new AudioFileReader(tempAudioPath))
                        {
                            double totalDuration = repeatedReader.TotalTime.TotalSeconds;
                            int totalFrames = (int)(Math.Ceiling(totalDuration * FPS));
                            var progressBarWidth = VIDEO_WIDTH - 100;
                            
                            log.Log($"开始生成视频帧，总帧数: {totalFrames}");
                            
                            // 加载并处理图片
                            using (var sourceImage = SKBitmap.Decode(wordInfo.ImagePath))
                            {
                                // 裁剪水印
                                var croppedImage = CropWatermark(sourceImage);
                                
                                // 生成每一帧
                                for (int frame = 0; frame < totalFrames; frame++)
                                {
                                    var framePath = Path.Combine(framesDir, $"frame_{frame:D6}.png");
                                    framesList.Add(framePath);

                                    using (var surface = SKSurface.Create(new SKImageInfo(VIDEO_WIDTH, VIDEO_HEIGHT)))
                                    {
                                        var canvas = surface.Canvas;
                                        
                                        // 绘制背景
                                        canvas.Clear(SKColors.White);
                                        
                                        // 绘制图片
                                        DrawImage(canvas, croppedImage);
                                        
                                        // 绘制翻译
                                        DrawTranslations(canvas, wordInfo.Translations);
                                        
                                        // 绘制进度条和文字
                                        DrawProgressBar(canvas, frame, totalFrames, progressBarWidth);
                                        
                                        // 绘制单词
                                        DrawWord(canvas, wordInfo.Word, frame);
                                        
                                        // 保存帧
                                        using (var frameImage = surface.Snapshot())
                                        using (var data = frameImage.Encode(SKEncodedImageFormat.Png, 100))
                                        {
                                            using (var stream = File.OpenWrite(framePath))
                                            {
                                                data.SaveTo(stream);
                                            }
                                        }
                                    }

                                    if (frame % FPS == 0)
                                    {
                                        log.Log($"已生成 {frame}/{totalFrames} 帧");
                                    }
                                }
                            }

                            log.Log("开始合成最终视频");
                            
                            // 3. 使用FFmpeg合成视频
                            var ffmpegArgs = FFMpegArguments
                                .FromFileInput(Path.Combine(framesDir, "frame_%06d.png"), false, options => options
                                    .WithFramerate(FPS))
                                .AddFileInput(tempAudioPath)
                                .OutputToFile(wordInfo.OutputVideoPath, true, options => options
                                    .WithVideoCodec("libx264")
                                    .WithConstantRateFactor(23)
                                    .WithAudioCodec("aac")
                                    .WithAudioBitrate(192)
                                    .WithVariableBitrate(4)
                                    .WithVideoFilters(filterOptions => filterOptions
                                        .Scale(VIDEO_WIDTH, VIDEO_HEIGHT))
                                    .WithFastStart()
                                    .ForcePixelFormat("yuv420p")
                                    .WithCustomArgument("-shortest"));

                            await ffmpegArgs.ProcessAsynchronously();
                            
                            log.Log($"视频生成完成: {wordInfo.OutputVideoPath}");
                        }
                    }
                    finally
                    {
                        // 清理临时文件
                        log.Log("清理临时文件");
                        if (File.Exists(tempAudioPath))
                            File.Delete(tempAudioPath);
                        if (Directory.Exists(framesDir))
                            Directory.Delete(framesDir, true);
                    }
                }
                catch (Exception ex)
                {
                    log.LogError($"生成视频时发生错误: {wordInfo.Word}", ex);
                    throw;
                }
            }
        }

        private ISampleProvider TrimSilence(AudioFileReader reader)
        {
            var silenceDetector = new SilenceDetector(reader, -40, TimeSpan.FromMilliseconds(100));
            var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
            var trimmedSamples = new List<float>();
            int read;

            bool hasStartedSound = false;
            int silenceCount = 0;
            const int silenceThreshold = 2000; // 约0.05秒的静音

            while ((read = silenceDetector.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    if (!silenceDetector.IsSilence(buffer[i]))
                    {
                        hasStartedSound = true;
                        silenceCount = 0;
                        trimmedSamples.Add(buffer[i]);
                    }
                    else if (hasStartedSound)
                    {
                        silenceCount++;
                        if (silenceCount < silenceThreshold)
                        {
                            trimmedSamples.Add(buffer[i]);
                        }
                    }
                }
            }

            return new TrimmedSampleProvider(reader.WaveFormat, trimmedSamples.ToArray());
        }

        private ISampleProvider RepeatAudio(ISampleProvider audio, int count)
        {
            // 创建一个新的缓冲区来存储所有样本
            var allSamples = new List<float>();
            var buffer = new float[8192];
            int read;

            // 读取所有样本
            while ((read = audio.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    allSamples.Add(buffer[i]);
                }
            }

            // 创建重复的样本数组
            var repeatedSamples = new float[allSamples.Count * count];
            for (int i = 0; i < count; i++)
            {
                allSamples.CopyTo(0, repeatedSamples, i * allSamples.Count, allSamples.Count);
            }

            return new RepeatedSampleProvider(audio.WaveFormat, repeatedSamples);
        }

        private class TrimmedSampleProvider : ISampleProvider
        {
            private readonly float[] _samples;
            private int _position;

            public TrimmedSampleProvider(WaveFormat waveFormat, float[] samples)
            {
                WaveFormat = waveFormat;
                _samples = samples;
            }

            public WaveFormat WaveFormat { get; }

            public int Read(float[] buffer, int offset, int count)
            {
                int availableSamples = Math.Min(count, _samples.Length - _position);
                Array.Copy(_samples, _position, buffer, offset, availableSamples);
                _position += availableSamples;
                return availableSamples;
            }
        }

        private class RepeatedSampleProvider : ISampleProvider
        {
            private readonly WaveFormat _waveFormat;
            private readonly float[] _samples;
            private int _position;

            public RepeatedSampleProvider(WaveFormat waveFormat, float[] samples)
            {
                _waveFormat = waveFormat;
                _samples = samples;
                _position = 0;
            }

            public WaveFormat WaveFormat => _waveFormat;

            public int Read(float[] buffer, int offset, int count)
            {
                var availableSamples = Math.Min(count, _samples.Length - _position);
                if (availableSamples > 0)
                {
                    Array.Copy(_samples, _position, buffer, offset, availableSamples);
                    _position += availableSamples;
                }
                return availableSamples;
            }
        }

        private class SilenceDetector : ISampleProvider
        {
            private readonly ISampleProvider _source;
            private readonly float _threshold;
            private readonly TimeSpan _minimumDuration;
            private readonly int _minimumSamples;

            public SilenceDetector(ISampleProvider source, float thresholdDb, TimeSpan minimumDuration)
            {
                _source = source;
                _threshold = (float)Math.Pow(10, thresholdDb / 20);
                _minimumDuration = minimumDuration;
                _minimumSamples = (int)(minimumDuration.TotalSeconds * source.WaveFormat.SampleRate);
            }

            public WaveFormat WaveFormat => _source.WaveFormat;

            public int Read(float[] buffer, int offset, int count)
            {
                return _source.Read(buffer, offset, count);
            }

            public bool IsSilence(float sample)
            {
                return Math.Abs(sample) < _threshold;
            }
        }

        private SKBitmap CropWatermark(SKBitmap image)
        {
            // 从四边进行裁剪，每边裁剪10%
            int cropX = (int)(image.Width * 0.1);
            int cropY = (int)(image.Height * 0.1);
            int newWidth = (int)(image.Width * 0.8);
            int newHeight = (int)(image.Height * 0.8);
            var info = new SKImageInfo(newWidth, newHeight);
            var croppedBitmap = new SKBitmap(info);
            
            using (var canvas = new SKCanvas(croppedBitmap))
            {
                canvas.DrawBitmap(image, 
                    new SKRect(cropX, cropY, cropX + newWidth, cropY + newHeight),
                    new SKRect(0, 0, newWidth, newHeight));
            }
            
            return croppedBitmap;
        }

        private void DrawImage(SKCanvas canvas, SKBitmap image)
        {
            // 在画布中央偏下位置绘制图片
            float scale = Math.Min((float)VIDEO_WIDTH / image.Width, (float)VIDEO_HEIGHT / 2 / image.Height);
            float width = image.Width * scale;
            float height = image.Height * scale;
            float x = (VIDEO_WIDTH - width) / 2;
            float y = VIDEO_HEIGHT * 0.6f - height / 2;
            
            canvas.DrawBitmap(image, new SKRect(x, y, x + width, y + height));
        }

        private void DrawWord(SKCanvas canvas, string word, int currentFrame)
        {
            const float MIN_DISTANCE = 80;
            const int MAX_ATTEMPTS = 50;

            if (currentFrame % (FPS / 5) == 0)
            {
                float textSize = 48;
                using (var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = textSize,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Bold),
                    TextAlign = SKTextAlign.Center
                })
                {
                    var textBounds = new SKRect();
                    paint.MeasureText(word, ref textBounds);

                    for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
                    {
                        float x = _random.Next((int)textBounds.Width, VIDEO_WIDTH - (int)textBounds.Width);
                        float y = _random.Next((int)(textBounds.Height + VIDEO_HEIGHT * 0.4f), 
                                            VIDEO_HEIGHT - (int)textBounds.Height);

                        bool overlaps = false;
                        foreach (var pos in _wordPositions)
                        {
                            if (Math.Sqrt(Math.Pow(x - pos.Position.X, 2) + Math.Pow(y - pos.Position.Y, 2)) < MIN_DISTANCE)
                            {
                                overlaps = true;
                                break;
                            }
                        }

                        if (!overlaps)
                        {
                            float rotation = (float)(_random.NextDouble() * 30 - 15);
                            _wordPositions.Add((new SKPoint(x, y), rotation));
                            break;
                        }
                    }
                }
            }

            using (var paint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 48,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Bold),
                TextAlign = SKTextAlign.Center
            })
            {
                foreach (var (position, rotation) in _wordPositions)
                {
                    canvas.Save();
                    canvas.RotateDegrees(rotation, position.X, position.Y);
                    canvas.DrawText(word, position.X, position.Y, paint);
                    canvas.Restore();
                }
            }
        }

        private void DrawTranslations(SKCanvas canvas, string[] translations)
        {
            string translation = translations.Length > 0 ? translations[0] : "";
            if (translation.Length > 6)
            {
                translation = translation.Substring(0, 6);
            }

            using (var paint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 48,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal)
            })
            {
                float y = VIDEO_HEIGHT * 0.3f;
                canvas.DrawText(translation, VIDEO_WIDTH / 2, y, paint);
            }
        }

        private void DrawProgressBar(SKCanvas canvas, int currentFrame, int totalFrames, int width)
        {
            // 调整进度条宽度和高度
            float progressBarWidth = VIDEO_WIDTH * 0.4f;
            float progressBarHeight = 40;
            float cornerRadius = 20;
            
            // 修正进度计算
            float progress = Math.Min((float)currentFrame / totalFrames, 1.0f);
            float x = (VIDEO_WIDTH - progressBarWidth) / 2;
            float y = VIDEO_HEIGHT * 0.15f;
            
            // 绘制进度条背景阴影
            using (var shadowPaint = new SKPaint 
            { 
                Color = SKColors.Black.WithAlpha(20),
                ImageFilter = SKImageFilter.CreateDropShadow(0, 4, 4, 4, SKColors.Black.WithAlpha(50))
            })
            {
                var shadowRect = SKRect.Create(x, y + 2, progressBarWidth, progressBarHeight);
                canvas.DrawRoundRect(shadowRect, cornerRadius, cornerRadius, shadowPaint);
            }
            
            // 绘制进度条背景
            using (var paint = new SKPaint 
            { 
                Color = SKColors.White,
                ImageFilter = SKImageFilter.CreateDropShadow(0, 1, 2, 2, SKColors.Black.WithAlpha(40))
            })
            {
                var backgroundRect = SKRect.Create(x, y, progressBarWidth, progressBarHeight);
                canvas.DrawRoundRect(backgroundRect, cornerRadius, cornerRadius, paint);
            }
            
            // 绘制进度条
            using (var paint = new SKPaint())
            {
                // 创建渐变色
                var shader = SKShader.CreateLinearGradient(
                    new SKPoint(x, y),
                    new SKPoint(x + progressBarWidth, y),
                    new[] { new SKColor(0, 122, 255), new SKColor(64, 156, 255) },
                    new[] { 0.0f, 1.0f },
                    SKShaderTileMode.Clamp
                );
                paint.Shader = shader;
                
                var progressRect = SKRect.Create(x, y, progressBarWidth * progress, progressBarHeight);
                canvas.DrawRoundRect(progressRect, cornerRadius, cornerRadius, paint);
            }
            
            // 绘制文字
            using (var paint = new SKPaint
            {
                TextSize = 36,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Bold)
            })
            {
                // 判断是否在最后一秒
                bool isLastSecond = (totalFrames - currentFrame) <= FPS;
                if (isLastSecond)
                {
                    paint.Color = new SKColor(76, 175, 80); // 使用绿色
                    canvas.DrawText("写入大脑成功", VIDEO_WIDTH / 2, y - 20, paint);
                }
                else
                {
                    paint.Color = SKColors.Black;
                    canvas.DrawText("强行写入大脑中", VIDEO_WIDTH / 2, y - 20, paint);
                }
            }
            
            // 绘制百分比
            using (var paint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 24,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal)
            })
            {
                string percentage = $"{(progress * 100):F0}%";
                canvas.DrawText(percentage, VIDEO_WIDTH / 2, y + progressBarHeight + 25, paint);
            }
        }
    }
} 