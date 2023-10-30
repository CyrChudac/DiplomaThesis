using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCreationCore; 
public record OnlyWalkCommand(Vector2 Position) : PatrolCommand(Position, false) {
	public override void StartExecution() {}
	public override bool ExecutionFinished => true;
}
