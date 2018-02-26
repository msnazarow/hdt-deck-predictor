﻿using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System;

namespace DeckPredictor
{
	public class Predictor
	{
		private List<Deck> _possibleDecks;
		private IOpponent _opponent;
		private bool _classDetected;

		public Predictor(IOpponent opponent, ReadOnlyCollection<Deck> metaDecks)
		{
			Log.Debug("Copying possible decks from the current meta");
			_possibleDecks = new List<Deck>(metaDecks);
			_opponent = opponent;
		}

		public void OnGameStart()
		{
			CheckOpponentClass();
		}

		public void OnOpponentDraw()
		{
			CheckOpponentClass();
		}

		public ReadOnlyCollection<Deck> GetPossibleDecks()
		{
			return new ReadOnlyCollection<Deck>(_possibleDecks);
		}

		public void OnOpponentPlay(Card cardPlayed)
		{
			Log.Debug("cardPlayed: " + cardPlayed);
			FilterAllRevealedCards();
		}

		public void OnOpponentHandDiscard(Card cardDiscarded)
		{
			Log.Debug("cardDiscarded: " + cardDiscarded);
			FilterAllRevealedCards();
		}

		public void OnOpponentDeckDiscard(Card cardDiscarded)
		{
			Log.Debug("cardDiscarded: " + cardDiscarded);
			FilterAllRevealedCards();
		}

		public void OnOpponentSecretTriggered(Card secretTriggered)
		{
			Log.Debug("secretTriggered: " + secretTriggered);
			FilterAllRevealedCards();
		}

		public void OnOpponentJoustReveal(Card cardRevealed)
		{
			Log.Debug("cardRevealed: " + cardRevealed);
			FilterAllRevealedCards();
		}

		public void OnOpponentDeckToPlay(Card cardPlayed)
		{
			Log.Debug("cardPlayed: " + cardPlayed);
			FilterAllRevealedCards();
		}

		private void CheckOpponentClass()
		{
			if (_classDetected)
			{
				return;
			}
			if (string.IsNullOrEmpty(_opponent.Class))
			{
				return;
			}
			// Only want decks for the opponent's class.
			_possibleDecks = _possibleDecks.Where(x => x.Class == _opponent.Class).ToList();
			_classDetected = true;
			Log.Info(_possibleDecks.Count + " possible decks for class " + _opponent.Class);
		}

		private void FilterAllRevealedCards()
		{
			bool changed = false;
			_possibleDecks = _possibleDecks
				.Where(deck =>
				{
					foreach (Card card in _opponent.KnownCards)
					{
						if (!card.IsCreated && card.Collectible &&
							deck.Cards.FirstOrDefault(x => x.Id == card.Id) == null)
						{
							Log.Debug("Filtering out a deck missing card: " + card);
							changed = true;
							return false;
						}
					}
					return true;
				}).ToList();
			if (changed)
			{
				Log.Info(_possibleDecks.Count + " possible decks");
			}
		}
	}

}
