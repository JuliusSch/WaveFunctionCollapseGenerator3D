using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Module;

public class PatternGenerator : MonoBehaviour
{
    private List<Module> _modules;
    private Dictionary<string, int> _patterns;
    private Dictionary<string, string> _info;

    [SerializeField]
    private string TemplateFolderName;
    [SerializeField]
    private bool GeneratePatterns;

    private List<string> fourWaysSymmetricModules;
    private List<string> twoWaySymmetricModules;

    private static Quaternion ROTATE_Y_0 = new(0, 0, 0, 1);
    private static Quaternion ROTATE_Y_90 = new(0, 0.7071f, 0, 0.7071f);
    private static Quaternion ROTATE_Y_180 = new(0, 1, 0, 0);
    private static Quaternion ROTATE_Y_270 = new(0, 0.7071f, 0, -0.7071f);

    private static Quaternion ROTATE_Y_360 = new(0, 0, 0, -1);
    private static Quaternion ROTATE_Y_450 = new(0, -0.7071f, 0, -0.7071f);
    private static Quaternion ROTATE_Y_540 = new(0, -1, 0, 0);
    private static Quaternion ROTATE_Y_630 = new(0, -0.7071f, 0, 0.7071f);

    private void OnValidate()
    {
        if (GeneratePatterns)
        {
            GeneratePatterns = false;

            ReadInfoFromFile();
            int[,,] template = ReadTemplateFromFile();
        
            _patterns = ExtractPatterns1x2(template);

            List<Dictionary<string, int>> rotatedPatterns = new() {
                ExtractPatterns1x2(RotateTemplate(template, Rotation.ROTATE_90)),
                ExtractPatterns1x2(RotateTemplate(template, Rotation.ROTATE_180)),
                ExtractPatterns1x2(RotateTemplate(template, Rotation.ROTATE_270))
            };

            foreach (Dictionary<string, int> patterns in rotatedPatterns)
            {
                foreach (var pattern in patterns)
                {
                    try
                    {
                        _patterns[pattern.Key] += pattern.Value;
                    } catch (KeyNotFoundException)
                    {
                        _patterns.Add(pattern.Key, pattern.Value);
                    }
                }
            }

            var repeats = _patterns.Where(p => p.Value > 1).ToList();
            OutputToFile();
        }
    }

    private void ReadInfoFromFile()
    {
        _info = new();
        string line;

        using StreamReader sr = new(Application.dataPath + "/Models/LevelTemplates/" + TemplateFolderName + "/info.txt");
        while ((line = sr.ReadLine()) != null)
        {
            string[] parts = line.Split(":");
            _info.Add(parts[0], parts[1]);
        }
        fourWaysSymmetricModules = _info["4 ways symmetric"].Split(',').ToList();
        twoWaySymmetricModules = _info["2 ways symmetric"].Split(',').ToList();
    }

    private int[,,] ReadTemplateFromFile()
    {
        int[,,] template = new int[int.Parse(_info["template size x"]), int.Parse(_info["template size z"]), int.Parse(_info["template size y"])];

        //_moduleFrequencies = new Dictionary<string, int>();
        _modules = new();
        _modules.Add(new Module("Air", 0, Rotation.ROTATE_0));
        _modules.Add(new Module("Unfilled", 1, Rotation.ROTATE_0));
        _modules.Add(new Module("Base", 2, Rotation.ROTATE_0));

        using (StreamReader sr = new(Application.dataPath + "/Models/LevelTemplates/" + TemplateFolderName + "/template_data.txt"))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] parts = line.Split("||");
                var rotationQuat = ConvertZToY(new Quaternion(float.Parse(parts[5]), float.Parse(parts[6]), float.Parse(parts[7]), float.Parse(parts[4])));
                var rotation = GetRotation(rotationQuat);

                if (!_modules.Any(x => x.Type == parts[0] && x.rotation == rotation))
                    _modules.Add(new Module(parts[0], _modules.Count(), rotation));

                var location = new Vector3Int((int)float.Parse(parts[1]) / int.Parse(_info["tile size x"]), (int)float.Parse(parts[3]) / int.Parse(_info["tile size z"]), (int)float.Parse(parts[2]) / int.Parse(_info["tile size y"]));
                template[location.x, location.y, location.z] = _modules.Where(x => x.Type == parts[0] && x.rotation == rotation).First().Id;
            }
        }
        return template;
    }

    private void OutputToFile()
    {
        using (StreamWriter writer = new(Application.dataPath + "/Models/LevelTemplates/" + TemplateFolderName + "/patterns.txt"))
            foreach (var pattern in _patterns)
                writer.WriteLine(pattern.Key + "||" + pattern.Value);

        using (StreamWriter writer = new(Application.dataPath + "/Models/LevelTemplates/" + TemplateFolderName + "/modules.txt"))
            foreach (var module in _modules)
                writer.WriteLine(module.Type + "||" + module.Id + "||" + module.Frequency + "||" + module.rotation);
    }

    public Rotation GetRotation(Quaternion q)
    {
        if (Equals(q, ROTATE_Y_0) || Equals(q, ROTATE_Y_360)) return Rotation.ROTATE_0;
        if (Equals(q, ROTATE_Y_90) || Equals(q, ROTATE_Y_450)) return Rotation.ROTATE_90;
        if (Equals(q, ROTATE_Y_180) || Equals(q, ROTATE_Y_540)) return Rotation.ROTATE_180;
        if (Equals(q, ROTATE_Y_270) || Equals(q, ROTATE_Y_630)) return Rotation.ROTATE_270;

        throw (new Exception("Invalid rotation found in template, please ensure all are precise multiples of 90 degrees about the vertical axis.")); 
    }
    public struct Pattern1x2 {
        public int pointA;
        public int pointB;
        public Vector3Int pointBPos;
    } 

    private Dictionary<string, int> ExtractPatterns1x2(int[,,] template)
    {
        Dictionary<Pattern1x2, int> patterns = new();
        var templateDimensions = new Vector3Int(template.GetLength(0), template.GetLength(1), template.GetLength(2));

        IterateArray(template, (templateIter, templateInd) => {
            var point = new Vector3Int(templateInd[0], templateInd[1], templateInd[2]);
            var neighbours = GetValidNeighbours(point, templateDimensions);

            foreach (var neighbour in neighbours)
            {
                var neighbourArr = new int[] { neighbour.x, neighbour.y, neighbour.z };
                var pattern = new Pattern1x2 { pointA = (int)templateIter.GetValue(templateInd), pointB = (int)templateIter.GetValue(neighbourArr), pointBPos = neighbour - point };
                try
                {
                    patterns[pattern]++;
                } catch (KeyNotFoundException)
                {
                    patterns.Add(pattern, 1);
                }
            }
        });
        return patterns.Select(p => p).ToDictionary(p => p.Key.pointA.ToString() + "," + p.Key.pointB.ToString() + "," + p.Key.pointBPos.ToString(), p => p.Value);
    }

    private List<Vector3Int> GetValidNeighbours(Vector3Int point, Vector3Int dimensions)
    {
        List<Vector3Int> neighbours = new() {
            point + Vector3Int.up,
            point + Vector3Int.down,
            point + Vector3Int.forward,
            point + Vector3Int.back,
            point + Vector3Int.left,
            point + Vector3Int.right,
            point + Vector3Int.up + Vector3Int.forward,
            point + Vector3Int.up + Vector3Int.back,
            point + Vector3Int.up + Vector3Int.left,
            point + Vector3Int.up + Vector3Int.right,
            point + Vector3Int.down + Vector3Int.forward,
            point + Vector3Int.down + Vector3Int.back,
            point + Vector3Int.down + Vector3Int.left,
            point + Vector3Int.down + Vector3Int.right,
            point + Vector3Int.forward + Vector3Int.left,
            point + Vector3Int.forward + Vector3Int.right,
            point + Vector3Int.back + Vector3Int.left,
            point + Vector3Int.back + Vector3Int.right,
        };

        for (int i = neighbours.Count - 1; i >= 0; i--)
        {
            var n = neighbours[i];
            if (n.x < 0 || n.x >= dimensions.x || n.y < 0 || n.y >= dimensions.y || n.z < 0 || n.z >= dimensions.z) neighbours.Remove(n);
        }
        return neighbours;
    }

    private int[,,] RotateTemplate(int[,,] array, Rotation rotation)
    {
        int[,,] rotatedArray = new int[0,0,0];

        for (int i = 0; i < (int) rotation; i++)
        {
            rotatedArray = new int[array.GetLength(2), array.GetLength(1), array.GetLength(0)];
            for (int z = 0; z < array.GetLength(2); z++)
            {
                for (int y = 0; y < array.GetLength(1); y++)
                {
                    for (int x = 0; x < array.GetLength(0); x++)
                    {
                        rotatedArray[z, y, array.GetLength(0) - 1 - x] = array[x, y, z];
                    }
                }
            }

            array = (int[,,])rotatedArray.Clone();
        }

        IterateArray(rotatedArray, (arrIter, arrIndices) => {
            rotatedArray.SetValue(GetRotatedId((int)rotatedArray.GetValue(arrIndices), rotation), arrIndices);
        });
        return rotatedArray;
    }

    public int GetRotatedId(int id, Rotation rotation)
    {
        var module = _modules.Where(m => m.Id == id).FirstOrDefault();
        if (module != null)
        {
            if (fourWaysSymmetricModules.Contains(module.Type))
                return module.Id;
            else if (twoWaySymmetricModules.Contains(module.Type))
                rotation = rotation == Rotation.ROTATE_180 ? Rotation.ROTATE_0 : rotation == Rotation.ROTATE_270 ? Rotation.ROTATE_90 : rotation;

            var match = _modules.Where(m => m.Type == module.Type && m.rotation == Rotate(module.rotation, rotation)).FirstOrDefault();
            if (match != null)
                return match.Id;
            else
            {
                var newId = _modules.Count();
                _modules.Add(new Module(module.Type, newId, Rotate(module.rotation, rotation)));
                return newId;
            }
        }
        return 0;
    }

    public Rotation Rotate(Rotation rot1, Rotation rot2) => (Rotation) (((int) rot1 + (int) rot2) % 4);

    public Quaternion ConvertZToY(Quaternion q) => new(q.x, q.z, q.y, q.w);

    delegate void IteratorDelegate(Array array, int[] indices);

    static void IterateArray(Array array, IteratorDelegate func)
    {
        int[] indices = new int[array.Rank];
        IterateArray(array, indices, 0, func);
    }

    static void IterateArray(Array array, int[] indices, int dimension, IteratorDelegate func)
    {
        int rank = array.Rank;
        int length = array.GetLength(dimension);

        if (dimension == rank - 1)
        {
            for (int i = 0; i < length; i++)
            {
                indices[dimension] = i;
                func(array, indices);
            }
        } else
        {
            for (int i = 0; i < length; i++)
            {
                indices[dimension] = i;
                IterateArray(array, indices, dimension + 1, func);
            }
        }
    }
}
