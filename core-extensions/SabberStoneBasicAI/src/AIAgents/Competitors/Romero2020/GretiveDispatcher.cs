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
 *		GretiveDispatcher.cs
 *
 *		It contains the definition and specification of the 'weight dispatcher' for the agent.
 *		Outside the game state, its only function is given a hero and a profile, assigning the corresponding set of pre-set weights.
 *		
 *		As you can see only a minimal set of profiles have been added, leaving the profile parameter practically irrelevant.
 *		It is understood that in the competition this parameter will not be used.
 *		
 *
 */


using SabberStoneCore.Enums;

namespace SabberStoneBasicAI.AIAgents.Gretive
{
	class GretiveDispatcher
	{

		public static GretiveScore Score(CardClass heroClass, Profile heroProfile)
		{
			switch (heroClass)
			{
				case CardClass.DRUID:
					switch (heroProfile)
					{
						case Profile.MIDRANGE:
						case Profile.DEFAULT_BY_HERO:
						default:
							return GretiveScoreGen(32.65355f, -47.92743f, 35.84834f, -48.95403f, 21.174f, -29.30753f, 52.53054f, -18.79501f, 27.23606f, -39.4745f);
					}
				case CardClass.HUNTER:
					switch (heroProfile)
					{
						case Profile.AGGRO:
						case Profile.DEFAULT_BY_HERO:
						default:
							return GretiveScoreGen(34.0909f, -48.18912f, 26.89414f, -33.90081f, 11.47699f, -44.57977f, 30.48743f, -46.80523f, 19.92627f, -42.86041f);
					}
				case CardClass.MAGE:
					switch (heroProfile)
					{
						case Profile.CONTROL:
						case Profile.DEFAULT_BY_HERO:
						default:
							return GretiveScoreGen(38.80242f, -92.0281f, 23.84993f, -22.77378f, 13.82049f, -52.32899f, 43.57949f, -45.75875f, 14.2201f, -27.63297f);
					}
				case CardClass.PALADIN:
					switch (heroProfile)
					{
						case Profile.MIDRANGE:
						case Profile.DEFAULT_BY_HERO:
						default:
							return GretiveScoreGen(48.19408f, -90.89896f, 22.04053f, - 40.43081f,  15.6046f, - 35.75431f,  48.71384f, -14.92025f,  44.28719f, -47.92072f);
					}
				case CardClass.PRIEST:
					switch (heroProfile)
					{
						case Profile.CONTROL:
						case Profile.DEFAULT_BY_HERO:
						default:
							return GretiveScoreGen(44.85069f, -48.65612f, 35.61387f, -42.27365f, 18.88283f, -94.16244f, 20.50913f, -36.56719f, 6.203781f, -20.33816f);
					}
				case CardClass.ROGUE:
					switch (heroProfile)
					{
						case Profile.AGGRO:
						case Profile.DEFAULT_BY_HERO:
						default:
							return GretiveScoreGen(31.03984f, -41.27439f, 36.42944f, -13.64148f, 53.96317f, -32.19125f, 8.117135f, -48.25733f, 26.03877f, -36.07202f);
					}
				case CardClass.SHAMAN:
					switch (heroProfile)
					{
						case Profile.CONTROL:
						case Profile.DEFAULT_BY_HERO:
						default:
							return GretiveScoreGen(38.80242f, -92.0281f, 23.84993f, -22.77378f, 13.82049f, -52.32899f, 43.57949f, -45.75875f, 14.2201f, -27.63297f);
					}
				case CardClass.WARLOCK:
					switch (heroProfile)
					{
						case Profile.MIDRANGE:
						case Profile.DEFAULT_BY_HERO:
						default:
							return GretiveScoreGen(38.80242f, -92.0281f, 23.84993f, -22.77378f, 13.82049f, -52.32899f, 43.57949f, -45.75875f, 14.2201f, -27.63297f);
					}
				case CardClass.WARRIOR:
					switch (heroProfile)
					{
						case Profile.AGGRO:
						case Profile.DEFAULT_BY_HERO:
						default:
							return GretiveScoreGen(42.93896f, -42.83433f, 12.74382f, -26.04122f, 31.2325f, -31.81688f, 50.74067f, -38.31769f, 29.32415f, -32.67503f);
							
					}
				default:
					switch (heroProfile)
					{
						case Profile.AGGRO:
							return GretiveScoreGen(34.0909f, -48.18912f, 26.89414f, -33.90081f, 11.47699f, -44.57977f, 30.48743f, -46.80523f, 19.92627f, -42.86041f);
						case Profile.CONTROL:
							return GretiveScoreGen(38.80242f, -92.0281f, 23.84993f, -22.77378f, 13.82049f, -52.32899f, 43.57949f, -45.75875f, 14.2201f, -27.63297f);
						case Profile.DEFAULT_BY_HERO:
						case Profile.MIDRANGE:
						default:
							return GretiveScoreGen(32.65355f, -47.92743f, 35.84834f, -48.95403f, 21.174f, -29.30753f, 52.53054f, -18.79501f, 27.23606f, -39.4745f);
					}
			}
		}

		public static GretiveScore GretiveScoreGen(float minTotalAtk, float opMinTotalAtk, float minTotalHth, float opMinTotalHth,
				float minTauntHth, float opMinTauntHth, float heroArmor, float opHeroArmor, float heroHp, float opHeroHp)
		{
			return new GretiveScore
			{
				W_MinionTotalAtk = minTotalAtk,
				W_OpMinionTotalAtk = opMinTotalAtk,
				W_MinionTotalHth = minTotalHth,
				W_OpMinionTotalHth = opMinTotalHth,
				W_MinionTauntHealth = minTauntHth,
				W_OpMinionTauntHealth = opMinTauntHth,
				W_HeroArmor = heroArmor,
				W_OpHeroArmor = opHeroArmor,
				W_HeroHp = heroHp,
				W_OpHeroHp = opHeroHp
			};
		}

	}
}
