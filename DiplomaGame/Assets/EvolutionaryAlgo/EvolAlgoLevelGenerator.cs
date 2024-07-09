using System.Collections.Generic;
using UnityEngine;
using EvolAlgoBase;
using GameCreatingCore;
using UnityEditor;
using System.Linq;
using System;
using GameCreatingCore.GamePathing;
using Unity.VisualScripting;
using GameCreatingCore.GameActions;
using GameCreatingCore.LevelRepresentationData;
using GameCreatingCore.StaticSettings;

public class EvolAlgoLevelGenerator : MonoBehaviour {
    [SerializeField] private int popSize = 100;
    [Tooltip("how many more offsprings are then population.")]
    [SerializeField] private float offspringMod = 1.5f;
    [SerializeField] private int generations = 100;
    [SerializeField] private float crossProb = 0.5f;
    [SerializeField] private float previousPopulationBias = 0.99f;
    [SerializeField] private int elitism = 1;
    [SerializeField] private EnemyMutator enemyEval;
    [SerializeField] private ObstaclesMutator obstaclesEval;
    [SerializeField] private LevelTester levelTester;
    [SerializeField] private PlayGroundBounds bounds;
    [SerializeField] private GameController gameController;
    [SerializeField] private string saveDirectory = "Levels/";
    [SerializeField] private string saveFailsDirectory = "24_FailedLevels/";
    [Header("mutation probabilities")]
    [Tooltip("The probablity of mutation occuring.")]
    [SerializeField] private float mutateProb = 0.05f;
    [Tooltip("Given the mutation occures, how probable it is that it will be this type of mutation relative to the other ones.")]
    [SerializeField] private int mutateObstsProb = 10;
    [Tooltip("Given the mutation occures, how probable it is that it will be this type of mutation relative to the other ones.")]
    [SerializeField] private int mutateEnemiesProb = 10;
    [Tooltip("Given the mutation occures, how probable it is that it will be this type of mutation relative to the other ones.")]
    [SerializeField] private int mutateOuterObstProb = 1;
    [Tooltip("Since the final playground is random, this specifies what how narrow it could be considering the bounds.")]
    [Range(0,1)]
    [SerializeField] private float boundsMinimalModifier = 0.3f;

    [SerializeField] private float killDistance = 3f;
    [SerializeField] private float killTime = 1.2f;

    [Range(0,1)]
    [SerializeField] private float rulletWheelSelectionEquate = 0.2f;

    

    const int randomSeed = -1;
    [Tooltip("-1 for a random seed.")]
    public int seed = randomSeed;

    [SerializeField]
    private int usedSeed;

    [ContextMenu(nameof(CreateLevels))]
    public void CreateLevels() {

        var utils = GetRandom();
        
    
        GenerationPercentsPlotter ploter = new GenerationPercentsPlotter(generations, generations, new DebugLogWriter(), false);
        int genPassed = 0;
        List<string> generationScores = new List<string>();
        void Plot(IEnumerable<Scored<LevelRepresentation>> generation) {
            ploter.Plot(generation);
            int gens = genPassed; //only to see the number while debugging
            var s = string.Join(", ", generation
                .Select(lr => lr.Score)
                .OrderByDescending(lr => lr)
                .Select(lr => lr.ToString().Replace(',', '.')));
            generationScores.Add(s);
            genPassed++;
            if(genPassed == generations) {
                Debug.Log(s);
            }
        }
        StaticGameRepresentation gameRules = gameController.GetStaticGameRepr();
        EvolAlgoParameters<LevelRepresentation> pars =
        new EvolAlgoParameters<LevelRepresentation>(
            popSize,
            (int)(popSize * offspringMod),
            //new RandomParentSelector<LevelRepresentation>(utils),
            new IterativeSelector<LevelRepresentation>(),
            new FunctionEvolBreeding<LevelRepresentation>(
                utils,
                1,
                1,
                crossProb,
                Cross,
                WithoutCross
            ),
            new Mutator<LevelRepresentation>(
                mutateProb,
                utils,
                i => Mutation(i, utils, gameRules.StaticMovementSettings.CharacterMaxRadius)
            ),
            new Selection<LevelRepresentation>(
                previousPopulationBias,
                elitism,
                utils,
                new RuletWheelSelection(rulletWheelSelectionEquate)
            ),
            GenerationsEndingPredicate.Get(generations),
            InitiatePop(utils),
            ScoreAndSaveFail,
            Plot
            );
        EvolutionaryAlgo<LevelRepresentation> evolAlgo =
            new EvolutionaryAlgo<LevelRepresentation>(pars);
        string dir = null;
        try {
            var result = evolAlgo.Run().ToList();
            dir = GetDirectory(saveDirectory);
            EnsureDirectoryExists(dir, '\\', '/');
            SaveLevels(result, dir);
            SaveText(generationScores, dir + "scores.txt");
        } finally {
            if(dir == null)
                dir = GetDirectory(saveFailsDirectory);
            EnsureDirectoryExists(dir, '\\', '/');
            SaveParameters(dir);
        }
    }

    private void SaveParameters(string dir) {
        SaveText(new object[] {
                "seed:", usedSeed,
                "mutation probability:", mutateProb,
                "generations", generations,
                "population size", popSize,
                "elitism", elitism,
                "previous population bias:", previousPopulationBias,
                "offspring modifier:", offspringMod,
                "crossover probability", crossProb,
                "mutate obsts:", mutateObstsProb,
                "mutate outer obst:", mutateOuterObstProb,
                "mutate enemies:", mutateEnemiesProb,
                "rullet wheel selection equate:", rulletWheelSelectionEquate,
            }.Select(x => x.ToString()), 
            dir + "pararams.txt");
    }

    private void SaveText(IEnumerable<string> scores, string path) {
        var currDir = System.IO.Directory.GetCurrentDirectory();
        currDir += "\\" + path;
        currDir = currDir.Replace('\\', System.IO.Path.DirectorySeparatorChar);
        currDir = currDir.Replace('/', System.IO.Path.DirectorySeparatorChar);
        System.IO.File.WriteAllLines(currDir, scores);
    }

    private EvolAlgoUtils GetRandom() {
        usedSeed = this.seed;
        if(usedSeed == randomSeed)
            usedSeed = (int)(UnityEngine.Random.value * 1_000_000);
        System.Random r = new System.Random(usedSeed);
        return new EvolAlgoUtils(new SystemRandomGenerator(r));
    }

    private float ScoreAndSaveFail(LevelRepresentation ind) {
        try {
            return levelTester.Score(ind);
        }catch (Exception e) {
            try {
                var dir = GetDirectory(saveFailsDirectory);
                EnsureDirectoryExists(dir, '\\', '/');
                SaveLevels(Enumerable.Empty<LevelRepresentation>().Append(ind), dir);
                throw;
            }catch(Exception e2) {
                throw new System.AggregateException(new Exception[] {e, e2});
            }
        }
    }

    private string GetDirectory(string saveDirectory) {
        var time = DateTime.Now;
        string lastDir = $"{time.Year % 1000}.{time.Month}.{time.Day}.{time.Hour}.{time.Minute}";
        return $"{saveDirectory}{lastDir}/";
    }

    private void EnsureDirectoryExists(string dir, params char[] separators) {
#if UNITY_EDITOR
        string[] dirs = new string[1];
        char usedSep = separators[0];
        foreach(var sep in separators) {
            dirs = dir.Split(sep);
            if(dirs.Length > 1) {
                usedSep = sep;
                break;
            }
        }
        for(int i = 0; i < dirs.Length; i++) {
            var curr = string.Join(usedSep, dirs.Take(i + 1));
            if(!AssetDatabase.IsValidFolder(curr)) {
                var pre = string.Join(usedSep, dirs.Take(i));
                AssetDatabase.CreateFolder(pre, dirs[i]);
            }
        }
#else
        throw new NotSupportedException("Saving levels is only supported in the editor.");
#endif
    }

    private void SaveLevels(IEnumerable<LevelRepresentation> levels, string dir) {
#if UNITY_EDITOR
        int i = 0;
        foreach(var lvl in levels) {
            var asset = ScriptableObject.CreateInstance<UnityLevelRepresentation>();
            asset.Enemies = lvl.Enemies;
            asset.Goal = lvl.Goal;
            asset.FriendlyStartPos = lvl.FriendlyStartPos;
            asset.Obstacles = lvl.Obstacles
                .Select(o => UnityObstacle.FromObstacle(o))
                .ToList();
            asset.OuterObstacle = UnityObstacle.FromObstacle(lvl.OuterObstacle);
            List<UnitySkillReprGrounded> toPickUp = new List<UnitySkillReprGrounded>();
            foreach(var pickupable in lvl.SkillsToPickup) {
                toPickUp.Add(new UnitySkillReprGrounded() {
                    groundedAt = pickupable.position,
                    unitySkillRepr = UnitySkillRepr.FromProvider(pickupable.action)
                });
            }
            asset.SkillsToPickup = toPickUp;
            List<UnitySkillRepr> usables = new List<UnitySkillRepr>();
            foreach(var provider in lvl.SkillsStartingWith) {
                usables.Add(UnitySkillRepr.FromProvider(provider));
            }
            asset.AvailableSkills = usables;
            string path = $"{dir}{i}.asset";
            AssetDatabase.CreateAsset(asset, path);
            i++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#else
        throw new NotSupportedException("Saving levels is only supported in the editor.");
#endif
    }

    [Header("Generated by the script, do not use.")]
    public List<UnityLevelRepresentation> created;

    private IEnumerable<LevelRepresentation> InitiatePop(EvolAlgoUtils utils) {
        created.Clear();
        while(true) {
            Vector3 start;
            Vector3 end;
            while(true) {
                start = utils.RandomVec(bounds.Rect);
                end = utils.RandomVec(bounds.Rect);
                var minX = MathF.Min(end.x, start.x);
                var minY = MathF.Min(end.y, start.y);
                var maxX = MathF.Max(end.x, start.x);
                var maxY = MathF.Max(end.y, start.y);
                if(maxX - minX > bounds.Rect.width * boundsMinimalModifier
                    && maxY - minY > bounds.Rect.height * boundsMinimalModifier) {
                    start = new Vector3(minX, minY);
                    end = new Vector3(maxX, maxY);
                    break;
                }
            }
            float radius = Mathf.Min(bounds.Rect.width * boundsMinimalModifier, bounds.Rect.height * boundsMinimalModifier);
            radius = (0.02f + 0.1f * utils.RandomFloat()) * radius;

            var goalChangeOffset =
                //(end - start).normalized * radius * (float)Mathf.Sqrt(2);
                new Vector3(radius, radius);
            var lr = new LevelRepresentation(
                new List<Obstacle>(),
                new Obstacle(
                    new List<Vector2>() {
                        start,
                        new Vector2(start.x, end.y),
                        end,
                        new Vector2(end.x, start.y)
                    },
                    new ObstacleEffect(
                    WalkObstacleEffect.Unwalkable,
                    WalkObstacleEffect.Unwalkable,
                    VisionObstacleEffect.NonSeeThrough,
                    VisionObstacleEffect.NonSeeThrough)),
                new List<Enemy>(),
                new List<PickupableActionProvider>(),
                new List<IActiveGameActionProvider>() 
                    { new KillActionProvider(killTime, killDistance)},
                start + goalChangeOffset,
                new LevelGoal(
                    end - goalChangeOffset,
                    radius
                )
            );
            var cre = ScriptableObject.CreateInstance<UnityLevelRepresentation>();
            cre.Obstacles = lr.Obstacles
                .Select(o => UnityObstacle.FromObstacle(o))
                .ToList();
            cre.OuterObstacle = UnityObstacle.FromObstacle(lr.OuterObstacle);
            cre.Enemies = lr.Enemies;
            cre.FriendlyStartPos = lr.FriendlyStartPos;
            cre.Goal = lr.Goal;
            cre.SkillsToPickup = new List<UnitySkillReprGrounded>();
            cre.AvailableSkills = new List<UnitySkillRepr>() { new UnitySkillRepr() { 
                maxUseDistance = killDistance,
                nonCancelTime = killTime,
                type = SkillType.Kill
            } };
            created.Add(cre);
            yield return lr;
        }
    }
    


    private IList<LevelRepresentation> Cross(IList<LevelRepresentation> parents) {
        return parents;
    }

    //since it's 2 -> 2, we can just return parents
    private IList<LevelRepresentation> WithoutCross(IList<LevelRepresentation> parents) {
        return parents;
    }

    private LevelRepresentation Mutation(LevelRepresentation ind, EvolAlgoUtils utils, float characterRadius) {
        var sum = mutateObstsProb + mutateEnemiesProb + mutateOuterObstProb;
        var curr = utils.RandomInt(sum);
        T DetermineMutation<T>(T input, int prob, Func<T, T> mutateFunc) {
            if(curr < prob) {
                input = mutateFunc(input);
                curr += sum;
            }
            curr -= prob;
            return input;
        }

        List<Vector2> enemyPoints = ind.Enemies
            .Select(e => e.Position)
            .Concat(ind.Enemies
                .Select(e => e.Path)
                .NotNull()
                .Select(p => p.Commands)
                .SelectMany(x => x)
                .Select(c => c.Position))
            .ToList();

        List<Vector2> playerPositions = Enumerable.Empty<Vector2>()
            .Append(ind.FriendlyStartPos)
            .Append(ind.Goal.Position)
            .ToList();

        var outerObst = DetermineMutation(ind.OuterObstacle, 
            mutateOuterObstProb, 
            oo => obstaclesEval.MutateOuterObstacle(oo, utils, enemyPoints, playerPositions, characterRadius));
        var obsts = DetermineMutation(ind.Obstacles,
            mutateObstsProb, 
            t => obstaclesEval.MutateObsts(t, utils, outerObst, enemyPoints, playerPositions, characterRadius));

        var enemyObsts = obsts
            .Where(o => o.Effects.EnemyWalkEffect == WalkObstacleEffect.Unwalkable)
            .Select(o => ObstaclesInflator.InflateNormal(o, characterRadius))
            .ToList();

        var enemies = DetermineMutation(ind.Enemies, 
            mutateEnemiesProb,
            t => enemyEval.MutateEnems(t, utils, ObstaclesInflator.InflateOuterObst(outerObst, characterRadius), enemyObsts));
        
        return new LevelRepresentation(
            obsts,
            outerObst,
            enemies,
            ind.SkillsToPickup,
            ind.SkillsStartingWith,
            ind.FriendlyStartPos,
            ind.Goal);
    }

    class IterativeSelector<T> : IParentSelector<T>, ISelectionMechanism  {
        private int index = 0;

		public List<Scored<T>> GetParents(List<Scored<T>> population, int count) {
            return Iterate(population, count, (u, i, j) => u.GetRange(i, j));
		}

        public void InitializeNewGeneration() => index = 0;

		public IList<Scored<T1>> Select<T1>(IEnumerable<Scored<T1>> from, int count, IRandomGen random) {
            return Iterate(from, count, (u, i, j) => u.Skip(i).Take(j).ToList());
		}

        private List<Scored<ElemType>> Iterate<EnumType, ElemType>(EnumType u, int count, Func<EnumType, int, int, List<Scored<ElemType>>> changeFunc) 
            where EnumType : IEnumerable<Scored<ElemType>> {
            var uCount = u.Count();
            var remaining = uCount - index;
            List<Scored<ElemType>> res;
            if(remaining >= count) {
                res = changeFunc(u, index, count); 
                index = (index + count) % uCount;
            } else {
                res = changeFunc(u, index, remaining);
                remaining = count - remaining;
                res.AddRange(changeFunc(u, 0, remaining));
                index = remaining;
            }
            return res;

        }
	}
}