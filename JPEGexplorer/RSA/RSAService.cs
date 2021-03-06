﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPEGexplorer.RSA
{
    public class RSAService
    {
        private static long LosujKlucze(long a = 1152921504606846976, long b = long.MaxValue)
        {
            Random random = new Random();

            while (czy_pierwsza(a) == false)
            {
                a = Math.Abs((long)(random.NextDouble() * long.MaxValue));
            }
            return a;
        }

        private static bool czy_pierwsza(long n)
        {
            if (n < 2)
                return false; //gdy liczba jest mniejsza niż 2 to nie jest pierwszą

            for (long i = 2; i * i <= n; i++)
                if (n % i == 0)
                    return false; //gdy znajdziemy dzielnik, to dana liczba nie jest pierwsza
            return true;
        }

        private static long NWD(long liczba1, long liczba2)
        {/*
            while (liczba1 != liczba2)
                if (liczba1 < liczba2) liczba2 -= liczba1; else liczba1 -= liczba2; 
            return liczba1;
            */
            long t;

            while (liczba2 != 0)
            {
                t = liczba2;
                liczba2 = liczba1 % liczba2;
                liczba1 = t;
            };
            return liczba1;
        }

        public static long[] ZnajdzWykladnikPublicznyiPrywatny(long p, long q)
        {
            long[] wykladniki = new long[2];
            long phi = (p - 1) * (q - 1);
            long wykladnikPubliczny = 0;

            for (long e = 3; NWD(e, phi) != 1; e += 2)
            {
                wykladnikPubliczny = e + 2;
            }
            long wykladnikPrywatny = Odwr_mod(wykladnikPubliczny, phi);

            wykladniki[0] = wykladnikPubliczny;
            wykladniki[1] = wykladnikPrywatny;

            return wykladniki;
        }

        private static long Odwr_mod(long a, long n)
        {
            long a0, n0, p0, p1, q, r, t;

            p0 = 0; p1 = 1; a0 = a; n0 = n;
            q = n0 / a0;
            r = n0 % a0;
            while (r > 0)
            {
                t = p0 - q * p1;
                if (t >= 0)
                    t = t % n;
                else
                    t = n - ((-t) % n);
                p0 = p1; p1 = t;
                n0 = a0; a0 = r;
                q = n0 / a0;
                r = n0 % a0;
            }
            return p1;
        }

        private static long PotegaModul(long wiadomosc, long wykladnikPubliczny, long modul)
        {
            long pot, wyn, q;
            pot = wiadomosc; wyn = 1;
            for (q = wykladnikPubliczny; q > 0; q /= 2)
            {
                if (Convert.ToBoolean(q % 2)) wyn = (wyn * pot) % modul;
                pot = (pot * pot) % modul; // kolejna potęga
            }
            return wyn;

        }

        public static byte[] Encode(byte[] data, long wykladnik, long modul)
        {
            long[] doKodowania = new long[data.Length];
            int incrementer = 0;
            long dana;
            foreach (byte liczba in data)
            {
                doKodowania[incrementer] = Convert.ToInt64(liczba);
                incrementer++;
            }

            incrementer = 0;

            foreach (long wiadomosc in doKodowania)
            {
                dana = wiadomosc;
                doKodowania[incrementer] = PotegaModul(dana, wykladnik, modul); //kodowanie liczb wiadomosc klucz prywatny modul
                incrementer++;
            }

            byte[] returned = new byte[data.Length * 8];

            for (int i = 0; i < doKodowania.Length; i++)
            {
                /*returned[i] = */BitConverter.GetBytes(doKodowania[i])/*[0]*/.CopyTo(returned, i * 8);
            }

            return returned;
        }

        public static byte[] Decode(byte[] coded, long wykladnikPrywatny, long modul)
        {
            if (coded.Length % 8 != 0)
                throw new ArgumentException("coded array length should be divisible by 8, as in 8 input bytes per 1 output byte");

            byte[] decoded = new byte[coded.Length / 8];

            for (int i = 0; i < decoded.Length; i++)
            {
                //byte[] bytes = new byte[8];
                //bytes[0] = coded[i];
                decoded[i] = Convert.ToByte(PotegaModul(BitConverter.ToInt64(coded, i * 8/*bytes, 0*/), wykladnikPrywatny, modul));
            }
            return decoded;
        }
    }
}


