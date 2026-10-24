using System;
using System.Collections.Generic;

namespace Models
{
    /// <summary>
    /// Singleton qui détermine l'année et la session courantes pour les
    /// prochaines inscriptions et allocations.
    ///
    /// Logique :
    ///   - Janvier  : on est en plein hiver, la prochaine session est encore Hiver.
    ///                  Sessions valides : 2, 4, 6 (sessions de la session d'hiver).
    ///   - Fév-Août : on prépare la session d'Automne de l'année courante.
    ///                  Sessions valides : 1, 3, 5.
    ///   - Sep-Déc  : on prépare la session d'Hiver de l'ANNÉE SUIVANTE.
    ///                  Sessions valides : 2, 4, 6, et Year++.
    ///
    /// Pourquoi des sessions impaires/paires ? Parce qu'au cégep, les cours de
    /// session 1, 3, 5 sont donnés à l'automne; les sessions 2, 4, 6 à l'hiver.
    /// </summary>
    public sealed class NextSession
    {
        public const int January = 1;  // mois limite pour considérer la session d'hiver courante
        public const int August = 8;   // mois limite pour considérer la session d'automne courante

        private static readonly NextSession instance = new NextSession();
        public static NextSession Instance => instance;

        // Modifiable par un employé du registraire qui change la session courante
        // depuis le pied de page (cf. requirement « Session courante » dans le PDF).
        public static DateTime CurrentDate = DateTime.Now;

        /// <summary>
        /// Retourne la liste des numéros de sessions valides pour la prochaine session.
        /// Sessions 1/3/5 = Automne, 2/4/6 = Hiver.
        /// </summary>
        public static List<int> ValidSessions
        {
            get
            {
                List<int> result = new List<int>();
                if (CurrentDate.Month > January && CurrentDate.Month <= August)
                {
                    // Fév à Août : on prépare l'Automne
                    result.Add(1);
                    result.Add(3);
                    result.Add(5);
                }
                else
                {
                    // Sep à Déc et Janvier : on prépare l'Hiver
                    result.Add(2);
                    result.Add(4);
                    result.Add(6);
                }
                return result;
            }
        }

        /// <summary>
        /// Année des prochaines inscriptions.
        /// Septembre à décembre, on prépare l'hiver de l'année SUIVANTE.
        /// </summary>
        public static int Year
        {
            get
            {
                int value = CurrentDate.Year;
                if (CurrentDate.Month > August && CurrentDate.Month <= 12) value++;
                return value;
            }
        }

        public static string ShortCaption => (ValidSessions.Contains(1) ? "Automne " : "Hiver ") + Year;
        public static string Caption => "Session " + ShortCaption;
    }
}
