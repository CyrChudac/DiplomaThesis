using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCreationCore; 
public interface ILevelCreator {
	LevelRepresentation CreateLevel(int seed);
}
