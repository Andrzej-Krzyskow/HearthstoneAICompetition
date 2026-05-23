/*
 * Copyright (c) 2020, Alejandro Romero.
 * All rights reserved.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or(at your option) any later version.
 *
 * Contributors:
 * Antonio M. Mora García
 * Dept. Signal Theory, Telematics and Communications.
 * University of Granada
 *
 *
 *		GretiveScore.cs
 *
 *		It contains the definition and specification of the 'score' class for the gretive agent.
 *		Its main function is to store and represent the weights applied in the calculation of the valuation of a game state at a given time.
 *		As a basis, a weight is neither positive nor negative with respect to the player, so it is the user of the instance who must manage that valuation.
 *		
 *
 */



using System;




namespace SabberStoneBasicAI.AIAgents.Gretive
{
	class GretiveScore : Score.Score
	{

		public virtual bool CheckVictory() => OpHeroHp < 1;

		public virtual bool CheckDefeat() => HeroHp < 1;

		public float W_HeroHp = 0;
		public float W_OpHeroHp = 0;
		public float W_HeroAtk = 0;
		public float W_OpHeroAtk = 0;
		public float W_HandCost = 0;
		public float W_HandCount = 0;
		public float W_OpHandCount = 0;
		public float W_DeckCount = 0;
		public float W_OpDeckCount = 0;
		public float W_MinionTotalAtk = 0;
		public float W_OpMinionTotalAtk = 0;
		public float W_MinionTotalHth = 0;
		public float W_OpMinionTotalHth = 0;
		public float W_MinionTauntHealth = 0;
		public float W_OpMinionTauntHealth = 0;
		public float W_HeroArmor = 0;
		public float W_OpHeroArmor = 0;

		public override int Rate()
		{
			if (CheckDefeat())
				return Int32.MinValue;
			if (CheckVictory())
				return Int32.MaxValue;
			
			float value = W_HeroHp * HeroHp + W_OpHeroHp * OpHeroHp + W_HeroAtk * HeroAtk + W_OpHeroAtk * OpHeroAtk +
				W_HandCost * HandTotCost + W_HandCount * HandCnt + W_OpHandCount * OpHandCnt +
				W_DeckCount * DeckCnt + W_OpDeckCount * OpDeckCnt + W_MinionTotalAtk * MinionTotAtk +
				W_MinionTotalHth * MinionTotHealth + W_OpMinionTotalAtk * OpMinionTotAtk +
				W_OpMinionTotalHth * OpMinionTotHealth + W_MinionTauntHealth * MinionTotHealthTaunt +
				W_OpMinionTauntHealth * OpMinionTotHealthTaunt + W_HeroArmor * HeroArmor +
				W_OpHeroArmor * OpHeroArmor;


			return (int)MathF.Round(value, 0);
		}

		public int HeroArmor => Controller.Hero.Armor;
		public int OpHeroArmor => Controller.Hero.Armor;

	}
}
