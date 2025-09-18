using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;
/// @author Mikko Karhunen
/// @version 12.04.2018
/// <summary>
/// Matopeli jossa liikutaan suoria viivoja nuolinäppäimillä ja kerätään pisteitä syömällä tonttuja.
/// Mato kasvaa pituudessa joka syönnin jälkeen.
/// </summary>
public class Matopeli : PhysicsGame
{
    private const int RUUDUN_KOKO = 60; //Pelialusta luodaan ruudukoksi.
    private const double NOPEUS = 0.2; //Madon nopeus.
    private const int LEVEYS = 21; //Kentän leveys.
    private const int KORKEUS = 15; //Kentän korkeus.
    private const int ALKUMATO = 4; //Alkumadon pituus.
    private int ENNATYS; 
    private int EDELLINEN_ENNATYS;
    private Timer AJASTIN;
    private IntMeter PISTEET;
    private IntMeter UUSI_ENNATYS;
    private GameObject RUOKA; //Madon syömä ruoka josta mato kasvaa pituudessaan.
    private Direction SUUNTA; //Madon suuntavektori
    private Direction VANHA_SUUNTA;
    private List<PhysicsObject> MADON_KEHO = new List<PhysicsObject>(); //Madon palaset. Pää on ensimmäinen.
    private EasyHighScore HUIPPUPELAAJAT= new EasyHighScore();


    /// <summary>
    /// Pelin aloitus.
    /// </summary>
    public override void Begin()
    {
        Level.BackgroundColor = Color.LightGray;
        AlkuValikko();
    }


    /// <summary>
    /// Aloitusvalikko pelille.
    /// </summary>
    private void AlkuValikko()
    {
        ClearAll();
        SUUNTA = Direction.None;
        MultiSelectWindow alkuValikko = new MultiSelectWindow("Snake? Snake!? Snaaaake!",
        "Aloita peli", "Huippu pelaajat", "Lopeta");
        Add(alkuValikko);
        alkuValikko.AddItemHandler(0, Aloita);
        alkuValikko.AddItemHandler(1, Huippu);
        alkuValikko.AddItemHandler(2, Exit);
    }


    /// <summary>
    /// Aloittaa ja rakentaa peli.
    /// </summary>
    private void Aloita()
    {
        EDELLINEN_ENNATYS = ENNATYS;
        IsPaused = false;
        MADON_KEHO.Clear();

        LuoAjastin();
        LuoKentta();
        LuoAlkuMato();
        LuoRuoka();

        PISTEET = LisaaLaskuri(Screen.Right - 80.0, Screen.Top - 50.0, 0, "Pisteet");
        UUSI_ENNATYS = LisaaLaskuri(Screen.Right - 80.0, Screen.Top - 20.0, ENNATYS, "Paras");

        Ohjaimet();
        Camera.ZoomToAllObjects();
    }

    /// <summary>
    /// Luo ajastimen.
    /// </summary>
    private void LuoAjastin()
    {
        AJASTIN = new Timer();
        AJASTIN.Interval = NOPEUS;
        AJASTIN.Timeout += Paivitys;
    }


    /// <summary>
    /// Luo kentän pelille.
    /// </summary>
    private void LuoKentta()
    {
        Level.BackgroundColor = Color.LightGray;
        Level.Width = RUUDUN_KOKO * LEVEYS;
        Level.Height = RUUDUN_KOKO * KORKEUS;
        Level.CreateBorders();
    }


    /// <summary>
    /// Luo alkumadon.
    /// </summary>
    private void LuoAlkuMato()
    {
        LuoPaa(Vector.Zero);
        MADON_KEHO[0].Image = LoadImage("piru");
        for (int i = 0; i < ALKUMATO - 1; i++)
        {
            LuoPaa(Vector.Zero);
        }
    }


    /// <summary>
    /// Luo matoon uuden palan.
    /// </summary>
    /// <param name="position"> Minne pala kentässä luodaan </param>
    /// <returns> Palauttaa matopalasen. </returns>
    private PhysicsObject LuoPaa(Vector position)
    {
        PhysicsObject paa = new PhysicsObject(RUUDUN_KOKO, RUUDUN_KOKO, Shape.Rectangle);
        paa.Color = Color.Black;
        paa.Position = position;
        paa.CanRotate = false;
        MADON_KEHO.Insert(0, paa);
        Add(paa);
        return paa;
    }


    /// <summary>
    /// Ohjelma, jota kutsutaan määrätyn ajan välein. Toteuttaa madon liikkumisen ja madon törmäämiset.
    /// </summary>
    private void Paivitys()
    {
        VANHA_SUUNTA = SUUNTA;
        PhysicsObject paa = MADON_KEHO[0];
        PhysicsObject vanhaPaa = MADON_KEHO[MADON_KEHO.Count - 1];
        paa.Position = vanhaPaa.Position + SUUNTA.GetVector() * (RUUDUN_KOKO);
        MADON_KEHO.RemoveAt(0);
        paa.Image = LoadImage("piru");
        vanhaPaa.Image = LoadImage("default");
        MADON_KEHO.Add(paa);
        
        if(RUOKA.IsInside(paa.Position))
        {
            PISTEET.Value++;
            if (ENNATYS < PISTEET)
            {
                UUSI_ENNATYS.Value++;
                ENNATYS = PISTEET;
            }
            MediaPlayer.Play("EAT");
            RUOKA.X = RandomGen.NextInt(-LEVEYS / 2, LEVEYS / 2) * RUUDUN_KOKO;
            RUOKA.Y = RandomGen.NextInt(-KORKEUS / 2, KORKEUS / 2) * RUUDUN_KOKO;

            LuoPaa(MADON_KEHO[MADON_KEHO.Count - 1].Position * -(RUUDUN_KOKO));
        }

        if (!Level.BoundingRect.IsInside(paa.Position))
        {
            MediaPlayer.Play("EXPLOSION");
            Kuolema();
            return;
        }

        for (int i = 1; i < MADON_KEHO.Count -1; i++)
        {
            if(MADON_KEHO[i].IsInside(paa.Position))
            {
                MediaPlayer.Play("D'OH");
                Kuolema();
                return;
            }
        }
    }


    /// <summary>
    /// Luo madolle ruuan.
    /// </summary>
    private void LuoRuoka()
    {
        //ruoka.X = rnd.Next(Level.Left + 10,Level.Right - 10);
        RUOKA = new PhysicsObject(RUUDUN_KOKO, RUUDUN_KOKO);
        RUOKA.X = RandomGen.NextInt(-LEVEYS/2,LEVEYS/2 )*RUUDUN_KOKO;
        RUOKA.Y = RandomGen.NextInt( -KORKEUS/2,KORKEUS/2 )*RUUDUN_KOKO;
        RUOKA.Image = LoadImage("tonttu");
        Add(RUOKA);
    }


    /// <summary>
    /// Luo laskurin peliin.
    /// </summary>
    /// <param name="x"> Laskurin paikka </param>
    /// <param name="y"> Laskurin paikka korkeus suunnassa </param>
    /// <param name="z"> Laskurin aloitus pisteet pelissä </param>
    /// <param name="otsikko"> Laskurin otsikko </param>
    /// <returns> Palauttaa pistelaskurin. </returns>
    private IntMeter LisaaLaskuri(double x, double y, int z, string otsikko)
    {
        IntMeter pistelaskuri = new IntMeter(z);
        Label naytto = new Label();
        naytto.BindTo(pistelaskuri);
        naytto.X = x;
        naytto.Y = y;
        naytto.TextColor = Color.Black;
        naytto.BorderColor = Color.Black;
        naytto.Color = Level.BackgroundColor;
        naytto.Title= otsikko;
        Add(naytto);
        return pistelaskuri;
    }


    /// <summary>
    /// Tekee peliin ohjaimet.
    /// </summary>
    private void Ohjaimet()
    {
        Keyboard.Listen(Key.Up, ButtonState.Pressed, AsetaSuunta, "Suunta ylös", Direction.Up);
        Keyboard.Listen(Key.Down, ButtonState.Pressed, AsetaSuunta, "Suunta alas", Direction.Down);
        Keyboard.Listen(Key.Left, ButtonState.Pressed, AsetaSuunta, "Suunta vasen", Direction.Left);
        Keyboard.Listen(Key.Right, ButtonState.Pressed, AsetaSuunta, "Suunta oikea", Direction.Right);

        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
    }


    /// <summary>
    /// Antaa madolle uuden suunnan. Ja käynnistää ajastimen.
    /// </summary>
    /// <param name="uusiSuunta"> Madolle asetettava uusi suunta </param>
    private void AsetaSuunta(Direction uusiSuunta)
    {
        if (SUUNTA.GetVector() == Vector.Zero)
        {
            AJASTIN.Start();
        }

        if (VANHA_SUUNTA.GetVector() == -uusiSuunta.GetVector()) return;

        else
        {
            SUUNTA = uusiSuunta;
        }
    }


    /// <summary>
    /// Käsittelee madon kuoleman.
    /// </summary>
    private void Kuolema()
    {
        if (PISTEET > EDELLINEN_ENNATYS) MediaPlayer.Play("Ta Da");
        IsPaused = true;
        HUIPPUPELAAJAT.EnterAndShow(PISTEET.Value);
        HUIPPUPELAAJAT.HighScoreWindow.Closed += delegate { AlkuValikko(); };
    }


    /// <summary>
    /// Antaa pelaaja top listan.
    /// </summary>
    private void Huippu()
    {
        HUIPPUPELAAJAT.Show();
        HUIPPUPELAAJAT.HighScoreWindow.Closed += delegate { AlkuValikko(); };
    }
}