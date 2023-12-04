﻿using Capstone.DAO;
using Capstone.Exceptions;
using Capstone.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("/animals")]
    public class AnimalController : ControllerBase
    {
        IAnimalDao animalDao;
        IImageDao imageDao;

        public AnimalController(IAnimalDao animalDao, IImageDao imageDao)
        {
            this.animalDao = animalDao;
            this.imageDao = imageDao;
        }

        [HttpGet]
        public ActionResult<List<Animal>> GetAnimals()
        {
            const string ErrorMessage = "There was an error";
            ActionResult result = BadRequest(new { message = ErrorMessage });

            try
            {
                List<Animal> animalList = animalDao.GetAnimals();

                result = Ok(imageDao.AddPicturesToListings(animalList));

            }
            catch (DaoException)
            {
                result = StatusCode(500, ErrorMessage);
            }

            return result;
        }
    }
}
