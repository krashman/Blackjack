﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Blackjack
{
    

    public class BlackjackGame: ICardGame
    {
        // The number of decks is 4
        public const int DECKS_COUNT = 4;
        protected Deck[] decks = new Deck[DECKS_COUNT];	            // Shoes, DECKS_COUNT decks

        // The number of players is 7 (tops)
        public const int MAX_PLAYERS = 7;

        protected CardHolder dealer = new CardHolder("Dealer");		// composite for the dealer
        protected List<Player> players = new List<Player>();		// aggregates the player
        
        private List<PlayerState> playerStates = new List<PlayerState>();

        IScoreCounter scoreCounter = new BlackjackScoreCounter();

        public int totalLose = 0;


        public bool dealerBlackjack = false;
        public bool dealerBust = false;



        public BlackjackGame()
        {
            dealer.SetScoreCounter( scoreCounter );

            for (int i = 0; i < DECKS_COUNT; i++)
                decks[i] = new Deck();

            totalLose = 0;
        }
	
	    public void addPlayer( Player p )
        {
            p.SetScoreCounter( scoreCounter );
            players.Add( p );
            playerStates.Add( PlayerState.HIT );
        }

        public Player GetPlayer(int pos)
        {
            return players[pos];
        }

        public CardHolder GetDealer()
        {
            return dealer;
        }

        public Deck GetDeck(int pos)
        {
            return decks[pos];
        }

        public int GetPlayersCount()
        {
            return players.Count;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="nPlayer"></param>
        /// <param name="state"></param>
        public void SetPlayerState(int nPlayer, PlayerState state)
        {
            if (state == PlayerState.DOUBLE)
            {
                if (players[nPlayer].CanDoubleStake())
                    players[nPlayer].BonusStake(2);
                else
                    throw new InvalidOperationException();
            }

            playerStates[nPlayer] = state;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="nPlayer"></param>
        /// <returns></returns>
        public PlayerState GetPlayerState(int nPlayer)
        {
            return playerStates[nPlayer];
        }
        

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool CheckStates()
        {
            for (int i = 0; i < players.Count; i++)
                if (GetPlayerState(i) != PlayerState.BUST &&
                    GetPlayerState(i) != PlayerState.STAND &&
                    GetPlayerState(i) != PlayerState.BLACKJACK)
                    return false;

            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        public bool CheckGameFinished()
        {
            for (int i = 0; i < players.Count; i++)
                if (players[i].PlayResult == PlayerResult.UNDEFINED)
                    return false;

            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="nPlayer"></param>
        /// <returns></returns>
	    public int PlayResults( int nPlayer )
        {
            // recalculate total lose for casino if some of the players loses

            
            // first check for player's bust
            if (GetPlayerState(nPlayer) == PlayerState.BUST)
            {
                players[nPlayer].LoseStake();
                totalLose -= players[nPlayer].Stake;
                return -1;
            }

            // сначала проверим и начислим, если нужно, бонусы игроку
            SetBonuses( nPlayer );


            // then check for dealer's bust
            if (dealer.CountScore() > 21)
            {
                players[nPlayer].WinStake();
                totalLose += players[nPlayer].Stake;
                return 1;
            }

            // далее обычное сравнение по очкам:

            // DEALER WINS
	        if ( players[ nPlayer ].CountScore() < dealer.CountScore() )
	        {
                players[ nPlayer ].LoseStake();
                totalLose -= players[nPlayer].Stake;
                return -1;
	        }
            // PLAYER WINS
	        else if ( players[ nPlayer ].CountScore() > dealer.CountScore() )
	        {
                if (dealer.PlayerHand.GetCardsNumber() > 1)
                {
                    players[nPlayer].WinStake();
                    totalLose += players[nPlayer].Stake;
                    return 0;
                }
                return 1;
	        }
            // STAY
            else
            {
                players[nPlayer].PlayResult = PlayerResult.STAY;
                //totalLose -= players[nPlayer].Stake;
                return 0;
            }
        }

	    

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nPlayer"></param>
	    public void SetBonuses( int nPlayer )
        {
            // отдельно рассматриваем случаи с блекджеком (там другие правила начисления выигрыша)
            if ( CheckBlackJack( players[ nPlayer ] ) )				
	        {
                // если при блекджеке игрока нет блекджека дилера,
                if ( !CheckBlackJack( dealer ) )					    
		        {
                    // то еще проверяем, по сколько у них карт:			
                    // если у дилера больше 1 карты, т.е. он пытался добрать карты, чтобы "перебить" блекджек игрока    
			        if ( dealer.PlayerHand.GetCardsNumber() > 1 )
                        // то выигрыш игрока должен быть 3 к 2 (по умолчанию)
                        players[nPlayer].BonusStake();

                    // также есть особая ситуация в самом начале (с раздачи):
                    // у дилера 1 карта, у игрока - 2
			        else if ( dealer.PlayerHand.GetCardsNumber() == 1				    
                                && players[nPlayer].PlayerHand.GetCardsNumber() == 2)
                        // в таком случае тоже выигрыш игрока равен 3 к 2
                        players[ nPlayer ].BonusStake();

                    // еще если три семерки, доп. выигрыш игроку (2 к 1)
                    if (Check777(players[ nPlayer ]) == true)				
			        {
                        players[ nPlayer ].BonusStake(2);
			        }
		        }
	        }
            // если блекджек был у дилера, то просто выводим эту информацию
	        if ( CheckBlackJack( dealer ) )				
	        {
                dealerBlackjack = true;
	        }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardHolder"></param>
        /// <returns></returns>
	    public bool CheckBlackJack( CardHolder cardHolder )
        {
            return (cardHolder.CountScore() == 21);
        }
	

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardHolder"></param>
        /// <returns></returns>
        public bool Check777( CardHolder cardHolder )
        {
            if (cardHolder.CountScore() == 21)
            {
                Hand hand = cardHolder.PlayerHand;

                if (hand.GetCardsNumber() == 3)
                    if (hand[0].Rank == 7 && hand[1].Rank == 7 && hand[2].Rank == 7)
                        return true;
            }

            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="nDeck"></param>
        /// <returns></returns>
        public Card PopCardFromDeck( out int nDeck )
        {
            Random r = new Random();
            nDeck = r.Next( DECKS_COUNT );
            return decks[nDeck].PopCard();
        }


        /// <summary>
        /// 
        /// </summary>
        public void Shuffle()
        {
            //
            dealerBlackjack = false;
            dealerBust = false;
            
            // обнулим хэнды
            dealer.ClearHand();				

            for (int i=0; i<players.Count; i++ )
            {
                players[i].ClearHand();
                SetPlayerState( i, PlayerState.HIT );
                players[i].PlayResult = PlayerResult.UNDEFINED;
            }

            // перемешаем случайно все колоды в шузах
            foreach (Deck d in decks)
		        d.Shuffle();

            totalLose = 0;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="nPlayer"></param>
        /// <param name="card"></param>
        public void PlayerHit( int nPlayer, Card card )
        {
            players[nPlayer].TakeCard( card );
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="nPlayer"></param>
        /// <returns></returns>
        public void DealerFirstHit( int nPlayer )
        {
            // самое хитрое тут: сразу проверяем, вдруг у игрока блекджек на 2 картах
            if (CheckBlackJack(players[nPlayer]))		
            {
                playerStates[nPlayer] = PlayerState.BLACKJACK;

                // тут еще проверка, что и у дилера может оказаться первой карта ценой 10 или 11
                // (но это нужно только для случая 2 карт у игрока в хэнде)

                if (players[nPlayer].PlayerHand.GetCardsNumber() == 2)
                {
                    if (dealer.CountScore() >= 10)
                    {
                        // если у дилера туз, то можно предложить игроку 2 таких варианта:
                        if (dealer.CountScore() == 11)
                        {
                            // можно просто взять выигрыш сразу (а если нет, то может быть выигрыш 3 к 2 (если у дилера не будет блекджека)
                            System.Windows.Forms.DialogResult res =
                                        System.Windows.Forms.MessageBox.Show(
                                        "Dealer's got Ace!",
                                        players[nPlayer].Name + ", would you like to take your win 1-to-1 or keep playing (in that case the dealer can also have 21 and you'll lose you stake)?",
                                        System.Windows.Forms.MessageBoxButtons.YesNo);

                            // если берем, то в этом случае сразу выходим отсюда
                            if (res == System.Windows.Forms.DialogResult.Yes)
                            {
                                players[nPlayer].WinStake();
                                totalLose += players[nPlayer].Stake;
                            }
                        }
                        // else continue
                    }
                    else
                    {
                        // если у игрока на 2 картах блекджек, а первая карта дилера меньше 10, то он сразу проигрывает (в схватке с данным игроком)
                        players[nPlayer].BonusStake();
                        players[nPlayer].WinStake();
                        totalLose += players[nPlayer].Stake;
                    }
                }
            }
        }
    }
}
