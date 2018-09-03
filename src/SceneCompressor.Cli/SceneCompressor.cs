using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Humanizer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SceneCompressor.Cli
{
    public class SceneCompressor
    {
        private readonly CompressionOptions _options;

        public SceneCompressor(CompressionOptions options)
        {
            _options = options;
        }

        public void Compress()
        {
            string inputStreamJson = File.ReadAllText(_options.Source.FullName, Encoding.UTF8);
            JObject inputSceneRoot = JObject.Parse(inputStreamJson);

            var stepLists = inputSceneRoot.SelectTokens("..steps").Cast<JArray>().ToList();

            foreach (JArray stepList in stepLists)
            {
                try
                {
                    var id = stepList.Parent.Parent.SelectToken(".id").Value<string>();

                    if (string.Equals(id, "AnimationPattern"))
                    {
                        continue;
                    }

                    var steps = new List<Step>();

                    for (int i = 0; i < stepList.Count; i++)
                    {
                        var compressed = stepList[i].SelectToken(".compressed")?.Value<bool>() ?? false;

                        var timeStep = stepList[i].SelectToken(".timeStep").Value<float>();

                        var pX = stepList[i].SelectToken(".position.x").Value<float>();
                        var pY = stepList[i].SelectToken(".position.y").Value<float>();
                        var pZ = stepList[i].SelectToken(".position.z").Value<float>();

                        var position = new Vector3(pX, pY, pZ);

                        var rX = stepList[i].SelectToken(".rotation.x").Value<float>();
                        var rY = stepList[i].SelectToken(".rotation.y").Value<float>();
                        var rZ = stepList[i].SelectToken(".rotation.z").Value<float>();
                        var rW = stepList[i].SelectToken(".rotation.w").Value<float>();

                        var rotation = new Quaternion(rX, rY, rZ, rW);

                        steps.Add(new Step(timeStep, position, rotation, compressed));
                    }

                    for (int p = 0; p < _options.Passes; p++)
                    {
                        var pass = new List<Step>();

                        for (int s = 1; s < steps.Count - 2; s += 2)
                        {
                            var s0 = steps[s - 1];
                            var s1 = steps[s];

                            if (!_options.Force && s0.Compressed)
                            {
                                // Already compressed, skip
                                pass.Add(s0);
                                pass.Add(s1);
                                continue;
                            }

                            var timeStep = (s1.Timestep - s0.Timestep) * 0.5f + s0.Timestep;
                            var pI = Vector3.Lerp(s0.Position, s1.Position, 0.5f);
                            var rI = Quaternion.Lerp(s0.Rotation, s1.Rotation, 0.5f);

                            pass.Add(new Step(timeStep, pI, rI, false));

                            if (_options.Verbose)
                                Console.WriteLine($"P:{p:00} S:{s:00000} T:{timeStep} P:{pI} R:{rI}");
                        }

                        steps = pass;
                    }

                    Console.WriteLine($"Compressed animation '{id}' {stepList.Count} -> {steps.Count} steps using {_options.Passes} passes OK.");

                    stepList.Clear();

                    foreach (Step step in steps)
                    {
                        stepList.Add(JToken.FromObject(new
                        {
                            timeStep = step.Timestep,
                            positionOn = true,
                            rotationOn = true,
                            position = new
                            {
                                x = step.Position.X,
                                y = step.Position.Y,
                                z = step.Position.Z
                            },
                            rotation = new
                            {
                                x = step.Rotation.X,
                                y = step.Rotation.Y,
                                z = step.Rotation.Z,
                                w = step.Rotation.W
                            },
                            compressed = true
                        }));
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Unexpected format for step list, skipping: {exception.Message}");
                }
            }

            if (_options.Target.Exists)
                _options.Target.Delete();

            using (var outputSceneStream = new FileStream(_options.Target.FullName, FileMode.OpenOrCreate, FileAccess.Write))
            using (var outputSceneStreamWriter = new StreamWriter(outputSceneStream))
            using (var outputSceneJsonWriter = new JsonTextWriter(outputSceneStreamWriter))
            {
                outputSceneJsonWriter.Formatting = Formatting.Indented;
                inputSceneRoot.WriteTo(outputSceneJsonWriter);
            }

            _options.Target.Refresh();

            Console.WriteLine($"Scene written to '{_options.Target.FullName}' OK, compressed {_options.Source.Length.Bytes().ToString("MB")} -> {_options.Target.Length.Bytes().ToString("MB")}.");

            // Look for associated image
            var sourceImagePath = new FileInfo(_options.Source.FullName.Replace(".json", ".jpg"));
            if (sourceImagePath.Exists)
            {
                var targetImagePath = new FileInfo(_options.Target.FullName.Replace(".json", ".jpg"));
                sourceImagePath.CopyTo(targetImagePath.FullName, true);
            }
            
            Console.WriteLine("Complete!");
        }
    }
}