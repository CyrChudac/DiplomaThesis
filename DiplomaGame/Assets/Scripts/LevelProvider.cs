using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using GameCreatingCore.LevelRepresentationData;
using GameCreatingCore.Commands;
using GameCreatingCore.GameActions;

public abstract class LevelProvider : MonoBehaviour
{
    public LevelRepresentation GetLevel(bool vocal)
        => BackwardsCompatibleLevel(GetLevelInner(vocal));

    protected abstract LevelRepresentation GetLevelInner(bool vocal);

    private LevelRepresentation BackwardsCompatibleLevel(LevelRepresentation level) {
        var enemies = level.Enemies.ToList();
        for(int i = 0; i < enemies.Count; i++) {
            if(enemies[i].Path != null) {
                Path? path;
                if(enemies[i].Path.Commands == null || enemies[i].Path.Commands.Count == 0)
                {
                    path = null;
                }else if(enemies[i].Path.Commands.Count == 1 
                    && enemies[i].Path.Commands[0] is OnlyWalkCommand) {
                    var c = enemies[i].Path.Commands[0];
                    path = new Path(true, new List<PatrolCommand>()
                        {new OnlyWaitCommand(c.Position, c.Running, c.TurnWhileMoving, c.TurningSide, 1)}
                        );
                }
                else
                {
                    path = enemies[i].Path;
                }
                enemies[i] = new Enemy(enemies[i].Position, enemies[i].Rotation, enemies[i].Type, path);
            }
        }
        return new LevelRepresentation(
            level.Obstacles,
            level.OuterObstacle,
            enemies,
            level.SkillsToPickup ?? new List<PickupableActionProvider>(),
            level.SkillsStartingWith ?? new List<IActiveGameActionProvider>(),
            level.FriendlyStartPos,
            level.Goal);
    }
}
