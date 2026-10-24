using Models;
using System;
using System.Web.Mvc;
using static Controllers.AccessControl;

namespace Controllers
{
    /// <summary>
    /// Contrôleur pour la gestion de la session courante (cf. PDF page 3).
    /// Permet à un usager du registraire de surcharger temporairement
    /// l'année et la session ciblée par NextSession (en cliquant sur le pied de page).
    /// </summary>
    public class SessionsController : Controller
    {
        /// <summary>
        /// Affiche le formulaire pour choisir l'année et la session courante.
        /// Pré-rempli avec les valeurs actuelles de NextSession.
        /// </summary>
        [UserAccess(Access.Write)]
        public ActionResult Edit()
        {
            ViewBag.PageTitle = "Session courrante";
            ViewBag.Year = NextSession.Year;
            // Si la session 1 est dans ValidSessions => période d'Automne ; sinon Hiver
            ViewBag.IsAutumn = NextSession.ValidSessions.Contains(1);
            return View();
        }

        /// <summary>
        /// Reçoit le choix de session. Pour modifier NextSession.Year et ValidSessions,
        /// on bricole la valeur de CurrentDate (la date "réelle" qui détermine la session).
        ///   Season = 1 (Automne) => choisir le 1er mai de l'année désirée
        ///                            (mois 5 ∈ [2..8] => Automne, et Year reste tel quel)
        ///   Season = 2 (Hiver)   => choisir le 15 janvier de l'année désirée
        ///                            (mois 1 ∉ [2..8] => Hiver, et mois 1 ∉ [9..12] => Year reste tel quel)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAccess(Access.Write)]
        public ActionResult Edit(int Year, int Season)
        {
            DateTime newCurrent;
            if (Season == 1) // Automne
            {
                newCurrent = new DateTime(Year, 5, 1);
            }
            else // Hiver
            {
                newCurrent = new DateTime(Year, 1, 15);
            }
            NextSession.CurrentDate = newCurrent;
            // Retour à la page précédente (souvent /Students/List)
            return Redirect(Request.UrlReferrer != null ? Request.UrlReferrer.ToString() : "/Students/List");
        }

        /// <summary>Bouton Annuler : retourne à la page précédente sans modifier la session.</summary>
        [UserAccess(Access.Write)]
        public ActionResult Cancel()
        {
            return Redirect(Request.UrlReferrer != null ? Request.UrlReferrer.ToString() : "/Students/List");
        }
    }
}
