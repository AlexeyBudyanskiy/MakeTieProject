﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MakeTie.Bll.Entities.Product;
using MakeTie.Bll.Exceptions;
using MakeTie.Bll.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace MakeTie.Web.Controllers
{
    [Route("api/[controller]")]
    public class RecommendationController : Controller
    {
        private const int MaxEntitiesCount = 3;
        private readonly IEntityAnalysisService _entityAnalysisService;
        private readonly IAssociationService _associationService;
        private readonly IProductService _productService;
        private readonly ILogger _logger;

        public RecommendationController(
            IEntityAnalysisService entityAnalysisService,
            IAssociationService associationService,
            IProductService productService,
            ILogger logger)
        {
            _entityAnalysisService = entityAnalysisService;
            _associationService = associationService;
            _productService = productService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string query)
        {
            var products = new List<Product>();
            try
            {
                var entities = await _entityAnalysisService.GetEntitiesAsync(query, MaxEntitiesCount);
                var stringEntities = entities.Select(entity => entity.Name);
                var associations = await _associationService.GetAssociationsAsync(stringEntities, MaxEntitiesCount);
                

                foreach (var association in associations)
                {
                    foreach (var item in association.Items)
                    {
                        products.AddRange(_productService.SearchProducts(item.Item)
                            .Where(product => product.ImageUrl != null)
                            .Take(3)
                            .ToList());
                    }
                }
            }
            catch (AssociationServiceException ex)
            {
                return ReturnServiceUnavailableResult(ex);
            }
            catch (ProductServiceException ex)
            {
                return ReturnServiceUnavailableResult(ex);
            }
            catch (EntityAnalysisServiceException ex)
            {
                return ReturnServiceUnavailableResult(ex);
            }

            return Ok(products);
        }

        private IActionResult ReturnServiceUnavailableResult(Exception ex)
        {
            _logger.Error(ex.Message, ex);

            return StatusCode((int)HttpStatusCode.ServiceUnavailable);
        }
    }
}