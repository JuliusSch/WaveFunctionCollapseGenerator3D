using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static Module;
using static PatternGenerator;
using Random = System.Random;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

public class WFCGenerator : MonoBehaviour
{
    Dictionary<Pattern1x2, int> _patterns;
    Pattern1x2[] _patternsArray;

    Dictionary<Vector3Int, List<int>> _patternsByRelPos;

    bool[,,][] _tileWaveforms;
    bool[,,][] _permissibleModules;
    private Vector3Int _tileSize = new(5, 3, 5);
    private List<Module> Modules;
    private int modulesCount;

    private static Quaternion ROTATE_0_Y = new(0, 0, 0, 1);
    private static Quaternion ROTATE_90_Y = new(0, -0.7071f, 0, -0.7071f);
    private static Quaternion ROTATE_180_Y = new(0, 1, 0, 0);
    private static Quaternion ROTATE_270_Y = new(0, -0.7071f, 0, 0.7071f);

    private readonly Dictionary<Rotation, Quaternion> RotationQuaternions = new() {
        { Rotation.ROTATE_0, ROTATE_0_Y }, { Rotation.ROTATE_90, ROTATE_90_Y },{ Rotation.ROTATE_180, ROTATE_180_Y },{ Rotation.ROTATE_270, ROTATE_270_Y }
    };

    [SerializeField]
    private Vector3Int LevelSize;
    [SerializeField]
    private string TemplateFolderName;
    [SerializeField]
    private List<GameObject> Models;

    void Start()
    {
        ReadPatternsFromFile();
        ReadModulesFromFile();
        GenerateLevel();

        var finalLevel = new int[LevelSize.x, LevelSize.y, LevelSize.z];

        IterateArray(finalLevel, (levelArray, levelIndices) => {
            var patterns = Enumerable.Range(0, _patternsArray.Length).Where(i => ((bool[])_tileWaveforms.GetValue(levelIndices))[i]).Select(i => _patternsArray[i].pointA).ToArray();
            var id = patterns.Count() == 1;
            levelArray.SetValue(patterns.Length == 1 ? patterns[0] : 1, levelIndices);
        });
        InstantiateLevel(finalLevel, LevelSize);
    }

    private void ReadPatternsFromFile()
    {
        var stringPatterns = new Dictionary<string, int>();
        _patterns = new();
        string line;
        
        using StreamReader sr = new(Application.dataPath + "/Models/LevelTemplates/" + TemplateFolderName + "/patterns.txt");
        while ((line = sr.ReadLine()) != null)
        {
            var parts = line.Split("||");
            stringPatterns.Add(parts[0], int.Parse(parts[1]));
        }
        foreach (var pattern in stringPatterns)
        {
            var parts = pattern.Key.Split(',');
            var bPos = new Vector3Int(int.Parse(parts[2].TrimStart('(')), int.Parse(parts[3]), int.Parse(parts[4].TrimEnd(')')));
            _patterns.Add(new Pattern1x2() { pointA = int.Parse(parts[0]), pointB = int.Parse(parts[1]), pointBPos = bPos }, pattern.Value);
        }

        _patternsArray = _patterns.Select(p => p.Key).ToArray();
      
        _patternsByRelPos = new() {
            { Vector3Int.up, _patterns.Where(p => p.Key.pointBPos == Vector3Int.up).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.down, _patterns.Where(p => p.Key.pointBPos == Vector3Int.down).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.left, _patterns.Where(p => p.Key.pointBPos == Vector3Int.left).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.right, _patterns.Where(p => p.Key.pointBPos == Vector3Int.right).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.forward, _patterns.Where(p => p.Key.pointBPos == Vector3Int.forward).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.back, _patterns.Where(p => p.Key.pointBPos == Vector3Int.back).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.up + Vector3Int.left, _patterns.Where(p => p.Key.pointBPos == Vector3Int.up + Vector3Int.left).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.up + Vector3Int.right, _patterns.Where(p => p.Key.pointBPos == Vector3Int.up + Vector3Int.right).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.up + Vector3Int.forward, _patterns.Where(p => p.Key.pointBPos == Vector3Int.up + Vector3Int.forward).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.up + Vector3Int.back, _patterns.Where(p => p.Key.pointBPos == Vector3Int.up + Vector3Int.back).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.down + Vector3Int.left, _patterns.Where(p => p.Key.pointBPos == Vector3Int.down + Vector3Int.left).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.down + Vector3Int.right, _patterns.Where(p => p.Key.pointBPos == Vector3Int.down + Vector3Int.right).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.down + Vector3Int.forward, _patterns.Where(p => p.Key.pointBPos == Vector3Int.down + Vector3Int.forward).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.down + Vector3Int.back, _patterns.Where(p => p.Key.pointBPos == Vector3Int.down + Vector3Int.back).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.forward + Vector3Int.left, _patterns.Where(p => p.Key.pointBPos == Vector3Int.forward + Vector3Int.left).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.forward + Vector3Int.right, _patterns.Where(p => p.Key.pointBPos == Vector3Int.forward + Vector3Int.right).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.back + Vector3Int.left, _patterns.Where(p => p.Key.pointBPos == Vector3Int.back + Vector3Int.left).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() },
            { Vector3Int.back + Vector3Int.right, _patterns.Where(p => p.Key.pointBPos == Vector3Int.back + Vector3Int.right).Select(p => Array.IndexOf(_patternsArray, p.Key)).ToList() }
        };
    }

    private void ReadModulesFromFile()
    {
        Modules = new();
        string line;

        using StreamReader sr = new(Application.dataPath + "/Models/LevelTemplates/" + TemplateFolderName + "/modules.txt");
        while ((line = sr.ReadLine()) != null)
        {
            string[] parts = line.Split("||");
            var rotation = (Rotation)Enum.Parse(typeof(Rotation), parts[3]);
            Modules.Add(new Module(parts[0], int.Parse(parts[1]), rotation));
        }
        modulesCount = Modules.Count;
    }

    private void InstantiateLevel(int[,,] level, Vector3Int levelSize)
    {
        IterateArray(level, (levelArray, levelIndices) => {
            int id = (int)levelArray.GetValue(levelIndices);
            if (id == 0) return;
            var p = levelIndices.ToArray();
            var module = Modules.Where(m => m.Id == id).FirstOrDefault();
            var model = Models.Where(m => m.name == module.Type).First();
            Instantiate(model, new Vector3((p[0] - levelSize.x / 2) * _tileSize.x, (p[1] - 0.8f) * _tileSize.y, (p[2] - levelSize.z / 2) * _tileSize.z), RotationQuaternions[module.rotation]);
        });
    }

    public void GenerateLevel()
    {
        SetInitialConditions();

        Random rand = new();
        List<Vector3Int> mostStable;

        while ((mostStable = GetLowestEntropyPoints()).Any())
        {
            var point = mostStable.ElementAt(rand.Next(mostStable.Count()));
            CollapsePoint(point);
            PropagateChange(point);
        }
    }

    private List<Vector3Int> GetLowestEntropyPoints()
    {
        int lowestEntropy = int.MaxValue;
        List<Vector3Int> result = new();

        IterateArray(_tileWaveforms, (tilesIter, tilesIndices) => {
            bool[] trueVals = ((bool[])tilesIter.GetValue(tilesIndices)).Where(v => v).ToArray();
            if (trueVals.Length < lowestEntropy && trueVals.Length > 1)
            {
                lowestEntropy = trueVals.Length;
                result = new();
            }
            if (trueVals.Length == lowestEntropy)
                result.Add(new Vector3Int(tilesIndices[0], tilesIndices[1], tilesIndices[2]));
        });
        return result;
    }

    private void CollapsePoint(Vector3Int p)
    {
        Random rand = new();
        var pointValues = _tileWaveforms[p.x, p.y, p.z];
        var possiblePatterns = Enumerable.Range(0, pointValues.Length).Where(i => pointValues[i]).ToArray();
        var totalOccurences = possiblePatterns.Select(i => _patterns.ElementAt(i).Value).Sum();
        var rando = rand.Next(totalOccurences);
        int runningSum = 0;
        foreach (var i in possiblePatterns)
        {
            runningSum += _patterns.ElementAt(i).Value;
            if (rando < runningSum)
            {
                CollapsePoint(p, i);
                return;
            }
        }
    }

    private void CollapsePoint(Vector3Int p, int index)
    {
        Array.Fill(_tileWaveforms[p.x, p.y, p.z], false);
        _tileWaveforms[p.x, p.y, p.z][index] = true;
        Array.Fill(_permissibleModules[p.x, p.y, p.z], false);
        _permissibleModules[p.x, p.y, p.z][_patternsArray[index].pointA] = true;
    }


    private void PropagateChange(Vector3Int point)
    {
        Queue<Vector3Int> updatedTiles = new();
        Vector3Int p;
        updatedTiles.Enqueue(point);
        int propCountThisChange = 0;

        while (updatedTiles.Any())
        {
            p = updatedTiles.Dequeue();
            var neighbours = GetNeighbours(p);

            foreach (var v in neighbours)
            {
                if (_tileWaveforms[v.x, v.y, v.z].Where(v => v).Count() == 1) continue;
                propCountThisChange++;
                if (HaveValidPatternsUpdated(v, p))
                    updatedTiles.Enqueue(v);
            }
        }
    }

    private void SetInitialConditions()
    {
        _tileWaveforms = new bool[LevelSize.x, LevelSize.y, LevelSize.z][];
        var trueArray = new bool[_patterns.Count];
        Array.Fill(trueArray, true);
        IterateArray(_tileWaveforms, (array, indices) => array.SetValue(trueArray.ToArray(), indices));
     
        _permissibleModules = new bool[LevelSize.x, LevelSize.y, LevelSize.z][];
        var trueArrayM = new bool[modulesCount];
        Array.Fill(trueArrayM, true);
        IterateArray(_permissibleModules, (array, indices) => array.SetValue(trueArrayM.ToArray(), indices));

        var baseId = Modules.Where(m => m.Type == "Base").First().Id;
        var defaultBase = _patternsArray.ToList().FindIndex(p => p.pointA == baseId);
        var airId = Modules.Where(m => m.Type == "Air").First().Id;
        var defaultAir = _patternsArray.ToList().FindIndex(p => p.pointA == airId);

        CollapsePoint(new Vector3Int(0, 0, 0), defaultBase);
        PropagateChange(new Vector3Int(0, 0, 0));
        CollapsePoint(new Vector3Int(1, 0, 0), defaultBase);
        PropagateChange(new Vector3Int(1, 0, 0));
        CollapsePoint(new Vector3Int(0, 0, 1), defaultBase);
        PropagateChange(new Vector3Int(0, 0, 1));
        CollapsePoint(new Vector3Int(0, LevelSize.y - 1, 0), defaultAir);
        PropagateChange(new Vector3Int(0, LevelSize.y - 1, 0));
        CollapsePoint(new Vector3Int(1, LevelSize.y - 1, 0), defaultAir);
        PropagateChange(new Vector3Int(1, LevelSize.y - 1, 0));
        CollapsePoint(new Vector3Int(0, LevelSize.y - 1, 1), defaultAir);
        PropagateChange(new Vector3Int(0, LevelSize.y - 1, 1));

        for (int x = 0; x < LevelSize.x; x++)
            for (int y = 0; y < LevelSize.y; y++)
                for (int z = 0; z < LevelSize.z; z++)
                {
                    _tileWaveforms[x, y, z] = _tileWaveforms[0, y, 0].ToArray();
                    _permissibleModules[x, y, z] = _permissibleModules[0, y, 0].ToArray();
                }
    }

    private List<Vector3Int> GetNeighbours(Vector3Int p)
    {
        if (_tileWaveforms[p.x, p.y, p.z].Where(n => n).Count() == 0)
            return new List<Vector3Int>();

        List<Vector3Int> neighbours = _patternsByRelPos.Keys.Select(k => k + p).ToList();

        for (int i = neighbours.Count - 1; i >= 0; i--)
        {
            var n = neighbours[i];
            if (n.x < 0 || n.x >= LevelSize.x || n.y < 0 || n.y >= LevelSize.y || n.z < 0 || n.z >= LevelSize.z || _tileWaveforms[n.x, n.y, n.z].Where(p => p).Count() == 0)
                neighbours.Remove(n);
        }
        return neighbours;
    }

    private bool HaveValidPatternsUpdated(Vector3Int checkingTile, Vector3Int sourceTile)
    {
        var o_in_c_space = sourceTile - checkingTile;
        bool hasUpdated = false;
        List<int> relevantPatterns = _patternsByRelPos[o_in_c_space].Where(i => _tileWaveforms[checkingTile.x, checkingTile.y, checkingTile.z][i]).ToList();

        foreach (var i in relevantPatterns)
            if (!_permissibleModules[sourceTile.x, sourceTile.y, sourceTile.z][_patternsArray[i].pointB])
            {
                hasUpdated = true;
                _tileWaveforms[checkingTile.x, checkingTile.y, checkingTile.z][i] = false;
            }

        if (hasUpdated)
        {
            var possibleIds = relevantPatterns.Where(i => _tileWaveforms[checkingTile.x, checkingTile.y, checkingTile.z][i]).Select(i => _patternsArray[i].pointA).ToHashSet().ToList();
            List<int> notPossibleIds = Enumerable.Range(0, modulesCount).ToList();
            foreach (int i in possibleIds)
                notPossibleIds.Remove(i);

            List<int> stillNotPossibleIds = new();
            foreach (int i in notPossibleIds)
            {
                if (_permissibleModules[checkingTile.x, checkingTile.y, checkingTile.z][i])
                {
                    _permissibleModules[checkingTile.x, checkingTile.y, checkingTile.z][i] = false;
                    stillNotPossibleIds.Add(i);
                }
            }
            for (int i = 0; i < _patternsArray.Length; i++)
            {
                if (!_tileWaveforms[checkingTile.x, checkingTile.y, checkingTile.z][i])
                    continue;
                if (stillNotPossibleIds.Contains(_patternsArray[i].pointA))
                    _tileWaveforms[checkingTile.x, checkingTile.y, checkingTile.z][i] = false;
            }
            if (notPossibleIds.Count() == modulesCount)
                return false;

            return true;
        }
        return false;
    }

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

[Serializable]
public class Module
{
    public enum Rotation { ROTATE_0, ROTATE_90, ROTATE_180, ROTATE_270 }

    public string Type;
    public int Id;
    public Rotation rotation;
    public int Frequency;

    public Module(string type, int id, Rotation rotation)
    {
        Type = type;
        Id = id;
        this.rotation = rotation;
        Frequency = 1;
    }
}