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
 *		Profile.cs
 *
 *		It contains the definition and specification of the profile names.
 *		It allows to establish a field added to the hero, with relevance in his game attitude.
 *		
 *
 */


namespace SabberStoneBasicAI.AIAgents.Gretive
{
	//More info:
	//https://hearthstone.gamepedia.com/Deck_type


	public enum Profile
	{
		DEFAULT_BY_HERO,
		AGGRO,
		CONTROL,
		MIDRANGE
	}
}
