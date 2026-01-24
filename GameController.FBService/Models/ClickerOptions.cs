namespace GameController.FBService.Models
{
	public class ClickerOptions
	{
		public int WindowSeconds { get; set; } = 10;            // sliding window TTL for sec buckets
		public int SameSecondThreshold { get; set; } = 10;      // how many unique users in 1 sec = burst
		public int WarnScore { get; set; } = 30;                // mark as suspect
		public int BlockScore { get; set; } = 160;              // later: auto block
        public int ConfirmScore { get; set; } = 100;              // later: auto block
        public bool BlockEnabled { get; set; } = false;         // observe-only by default
        public bool ConfirmEnabled { get; set; } = false; 
        public int CooldownHugToleranceSeconds { get; set; } = 2;   // +- sec
		public int CooldownHugPoints { get; set; } = 15;            // risk points per hit
		public int CooldownHugHitsToFlag { get; set; } = 3;         // how many near-cooldown hits to add flag

		public int AfterCooldownWindowSeconds { get; set; } = 5;     // 2-5 წამი; default 5
		public int AfterCooldownBurstThreshold { get; set; } = 8;    // რამდენი unique user ამ window-ში = burst
		public int AfterCooldownBurstPoints { get; set; } = 20;      // რამდენ ქულას ვუმატებთ ამ burst-ზე


		// Phase C v4: Cooldown Rhythm Band
		public int RhythmMinSamples { get; set; } = 8;               // მინიმუმ რამდენი ინტერვალი გვინდა
		public int RhythmBandMaxExtraSeconds { get; set; } = 90;     // cooldown + 90s-მდე ვთვლით “ბანდში”
		public int RhythmMaxMadSeconds { get; set; } = 2;            // თუ mad <= 2 წამი => ძალიან სტაბილურია
		public double RhythmEmaAlpha { get; set; } = 0.25;           // EMA update speed (0.2–0.3 კარგია)
		public int RhythmPoints { get; set; } = 25;                  // რამდენ ქულას დავუმატებთ ერთ “დაჭერაზე”
		public int RhythmHitsToFlag { get; set; } = 2;               // რამდენჯერ უნდა დადასტურდეს რომ ვფლაგოთ


        public int NameDuplicateTokenPoints { get; set; } = 5;       // "Mari Mari", "თენგო თენგო"
        public int NameAllDigitsPoints { get; set; } = 10;           // "23425"
        public int NameAlnumShortPoints { get; set; } = 6;           // "dato9"
        public int NameTooShortPoints { get; set; } = 4;             // "aa"


        public int NameLowAlphaRatioPoints { get; set; } = 5;        // ბევრი ციფრი/სიმბოლო

        public int NameMinLength { get; set; } = 3;                  // თუ <3 -> too short
        public int NameAlnumShortMaxLength { get; set; } = 12;       // "dato9" ტიპისთვის
        public double NameMinAlphaRatio { get; set; } = 0.60;        // letters/(letters+digits) < 0.60 => low-alpha-ratio

        // Combo points: only when name is low-quality AND we already have strong behavior
        public int NameComboCooldownHugPoints { get; set; } = 10;    // NAME + COOLDOWN_HUGGING
        public int NameComboRhythmBandPoints { get; set; } = 20;     // NAME + RHYTHM_BAND
        public int NameComboAfterWindowBurstPoints { get; set; } = 15; // NAME + MANY_USERS_AFTER_COOLDOWN_WINDOW




    }
}
