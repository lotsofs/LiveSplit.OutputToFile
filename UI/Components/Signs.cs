namespace LiveSplit.UI.Components.OutputToFile {
	// The values written to PreviousSplit\{level}\{method}\{comparison}\Sign.txt. After the finish, each run-time value has Finished in front.
	enum Signs {
		NotApplicable,
		Ahead,
		Behind,
		GainedAhead,
		GainedBehind,
		LostAhead,
		LostBehind,
		GoldAhead,
		GoldBehind,
		FinishedNotApplicable,
		FinishedAhead,
		FinishedBehind,
		FinishedGainedAhead,
		FinishedGainedBehind,
		FinishedLostAhead,
		FinishedLostBehind,
		FinishedGoldAhead,
		FinishedGoldBehind,
	}
}
