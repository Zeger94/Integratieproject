﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL;
using Domain.Entiteit;
using System.Web;

namespace BL
{
    public class EntiteitManager : IEntiteitManager
    {
        private IEntiteitRepository entiteitRepository = new EntiteitRepository();
        private UnitOfWorkManager uowManager;

        public EntiteitManager()
        {

        }

        public EntiteitManager(UnitOfWorkManager uofMgr)
        {
            uowManager = uofMgr;
        }

        public void initNonExistingRepo(bool withUnitOfWork = false)
        {
            // Als we een repo met UoW willen gebruiken en als er nog geen uowManager bestaat:
            // Dan maken we de uowManager aan en gebruiken we de context daaruit om de repo aan te maken.

            if (withUnitOfWork)
            {
                if (uowManager == null)
                {
                    uowManager = new UnitOfWorkManager();
                    entiteitRepository = new EntiteitRepository(uowManager.UnitOfWork);
                }
            }
            // Als we niet met UoW willen werken, dan maken we een repo aan als die nog niet bestaat.
            else
            {
                entiteitRepository = (entiteitRepository == null) ? new EntiteitRepository() : entiteitRepository;
            }
        }

        public void CreateTestData()
        {
            initNonExistingRepo(false);
            Domain.Entiteit.Organisatie NVA = new Domain.Entiteit.Organisatie()
            {
                Leden = new List<Domain.Entiteit.Persoon>(),
                Naam = "N-VA"
            };

            Domain.Entiteit.Persoon BenWeyts = new Domain.Entiteit.Persoon()
            {
                Naam = "Ben Weyts",
                Organisaties = new List<Domain.Entiteit.Organisatie>()
            };

            Domain.Entiteit.Persoon Bartje = new Domain.Entiteit.Persoon()
            {
                Naam = "Bart De Wever",
                Organisaties = new List<Domain.Entiteit.Organisatie>()
            };

            //legt eveneens relatie van organisatie -> lid (Ben Weyts) en van Ben Weyts kunnen we zijn orginasaties opvragen (in dit geval N-VA)
            BenWeyts.Organisaties.Add(NVA);
            Bartje.Organisaties.Add(NVA);


            entiteitRepository.AddEntiteit(NVA);
            entiteitRepository.AddEntiteit(BenWeyts);
            entiteitRepository.AddEntiteit(Bartje);
        }

        public List<Entiteit> getAlleEntiteiten()
        {
            initNonExistingRepo(false);
            return entiteitRepository.getAlleEntiteiten();
        }

        public void updateEntiteit(Entiteit entiteit)
        {
            initNonExistingRepo(false);
            entiteitRepository.updateEntiteit(entiteit);
        }

        public void AddThema(string naam, List<Sleutelwoord> sleutelwoorden)
        {
            Thema thema = new Thema()
            {
                Naam = naam,
                SleutenWoorden = sleutelwoorden
            };
            entiteitRepository.CreateThema(thema);
        }


        public void UpdateThema(Thema thema)
        {
            entiteitRepository.UpdateThema(thema);
        }

        public void DeleteThema(int entiteitsId)
        {
            entiteitRepository.DeleteThema(entiteitsId);
        }

        public IEnumerable<Thema> GetThemas()
        {
            return entiteitRepository.ReadThemas();
        }

        public Thema GetThema(int entiteitsId)
        {
            return entiteitRepository.ReadThema(entiteitsId);
        }

        #region
        public void AddPerson(Persoon p, HttpPostedFileBase ImageFile)
        {
            initNonExistingRepo(false);
            if (ImageFile != null)
            {
                entiteitRepository.CreatePersonWithPhoto(p, ImageFile);
            } else
            {
                entiteitRepository.CreatePersonWithoutPhoto(p);
            }
        }

        public Persoon ChangePerson(Persoon ChangedPerson)
        {
            initNonExistingRepo(false);
            Persoon toUpdated = GetPerson(ChangedPerson.EntiteitId);

            toUpdated.FirstName = ChangedPerson.FirstName;
            toUpdated.LastName = ChangedPerson.LastName;
            foreach (Organisatie o in toUpdated.Organisations)
            {
                ChangeOrganisatie(o);
            }
            return entiteitRepository.UpdatePerson(toUpdated);
        }

        public List<Persoon> GetAllPeople()
        {
            initNonExistingRepo(false);
            return entiteitRepository.ReadAllPeople().ToList();
        }

        public Persoon GetPerson(int id)
        {
            initNonExistingRepo(false);
            return entiteitRepository.ReadPerson(id);
        }

        public void RemovePerson(int id)
        {
            initNonExistingRepo(false);
            entiteitRepository.DeletePerson(id);
        }
        #endregion


        #region
        
        public void AddOrganisatie(Organisatie o, HttpPostedFileBase ImageFile)
        {
            initNonExistingRepo(false);
            if (ImageFile != null)
            {
                entiteitRepository.CreateOrganisatieWithPhoto(o, ImageFile);
            } else
            {
                entiteitRepository.CreateOrganisatieWithoutPhoto(o);
            }
        }

        public Organisatie ChangeOrganisatie(Organisatie ChangedOrganisatie)
        {
            initNonExistingRepo(false);
            Organisatie toUpdate = GetOrganisatie(ChangedOrganisatie.EntiteitId);
            toUpdate.Naam = ChangedOrganisatie.Naam;
            toUpdate.Gemeente = ChangedOrganisatie.Gemeente;
            toUpdate.Posts = ChangedOrganisatie.Posts;
            toUpdate.Trends = ChangedOrganisatie.Trends;

            return entiteitRepository.UpdateOrganisatie(ChangedOrganisatie);
        }

        public List<Organisatie> GetAllOrganisaties()
        {
            initNonExistingRepo(false);
            return entiteitRepository.ReadAllOrganisaties().ToList();
        }

        public Organisatie GetOrganisatie(int id)
        {
            initNonExistingRepo(false);
            return entiteitRepository.ReadOrganisatie(id);
        }

        public void RemoveOrganisatie(int id)
        {
            initNonExistingRepo(false);
            entiteitRepository.DeleteOrganisatie(id);
        }

        public Organisatie ChangeOrganisatie(Organisatie ChangedOrganisatie, IEnumerable<string> selectedPeople)
        {
            initNonExistingRepo(false);
            Organisatie toUpdate = GetOrganisatie(ChangedOrganisatie.EntiteitId);


            List<Persoon> NewlyAppointedPeople = new List<Persoon>();
            //Bestaande referenties verwijderen
            if (toUpdate.Leden != null)
            {

                foreach (Persoon p in toUpdate.Leden)
                {
                    p.Organisations.Remove(toUpdate);
                }
                toUpdate.Leden = new List<Persoon>();
            }

            //Nieuwe referenties toevoegen
            foreach (string pId in selectedPeople)
            {
                Persoon person = GetPerson(Int32.Parse(pId));
                //person.Organisations.Add(UpdatedOrganisatie);
                toUpdate.Leden.Add(person);
            }

            toUpdate.Naam = ChangedOrganisatie.Naam;
            toUpdate.Gemeente = ChangedOrganisatie.Gemeente;
            toUpdate.Posts = ChangedOrganisatie.Posts;
            toUpdate.Trends = ChangedOrganisatie.Trends;

            toUpdate.AantalLeden = toUpdate.Leden.Count();

            return entiteitRepository.UpdateOrganisatie(toUpdate);
        }

        public void ChangePerson(Persoon changedPerson, IEnumerable<string> selectedOrganisations)
        {
            initNonExistingRepo(false);
            Persoon toUpdated = GetPerson(changedPerson.EntiteitId);

            //Remove all references
            toUpdated.Organisations = new List<Organisatie>();

            //Add new References
            foreach (string oId in selectedOrganisations)
            {
                toUpdated.Organisations.Add(GetOrganisatie(Int32.Parse(oId)));
            }

            toUpdated.FirstName = changedPerson.FirstName;
            toUpdated.LastName = changedPerson.LastName;

            entiteitRepository.UpdatePerson(toUpdated);
        }

        public byte[] GetPersonImageFromDataBase(int id)
        {
            initNonExistingRepo(false);
            return entiteitRepository.GetPersonImageFromDataBase(id);
        }

        public byte[] GetOrganisationImageFromDataBase(int id)
        {
            initNonExistingRepo(false);
            return entiteitRepository.GetOrganisationImageFromDataBase(id);
        }

        public void DeleteSleutelwoord(int sleutelId)
        {
            entiteitRepository.DeleteSleutelwoord(sleutelId);
        }

        public Sleutelwoord GetSleutelwoord(int sleutelId)
        {
            return entiteitRepository.readSleutelwoord(sleutelId);
        }
        #endregion

     
    }
}
