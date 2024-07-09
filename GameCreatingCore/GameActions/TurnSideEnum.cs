using System;

namespace GameCreatingCore.GameActions {
	public enum TurnSideEnum {
        ShortestPrefereClockwise,
        ShortestPrefereAntiClockwise,
        Clockwise,
        Anticlockwise
    }

    public static class TurnSideEnum_Extensions {
        public static TurnSideEnum Opposite(this TurnSideEnum side) {
			switch(side) {
				case TurnSideEnum.ShortestPrefereClockwise:
					return TurnSideEnum.ShortestPrefereAntiClockwise;
				case TurnSideEnum.ShortestPrefereAntiClockwise:
					return TurnSideEnum.ShortestPrefereClockwise;
				case TurnSideEnum.Clockwise:
					return TurnSideEnum.Anticlockwise;
				case TurnSideEnum.Anticlockwise:
					return TurnSideEnum.Clockwise;
                default:
                    throw new NotImplementedException($"{nameof(Opposite)} for " +
                        $"{nameof(TurnSideEnum)} with value {side} not implemented.");
			}
		}
	}
}
