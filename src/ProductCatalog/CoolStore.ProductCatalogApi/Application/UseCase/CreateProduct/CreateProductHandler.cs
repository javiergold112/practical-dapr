﻿using System;
using System.Threading;
using System.Threading.Tasks;
using CoolStore.ProductCatalogApi.Domain;
using CoolStore.ProductCatalogApi.Infrastructure.Persistence;
using CoolStore.Protobuf.ProductCatalog.V1;
using MediatR;
using Microsoft.EntityFrameworkCore;
using N8T.Infrastructure;
using N8T.Infrastructure.Data;

namespace CoolStore.ProductCatalogApi.Application.UseCase.CreateProduct
{
    public class ProductCreatedHandler : IRequestHandler<CreateProductRequest, CreateProductResponse>
    {
        private readonly ProductCatalogDbContext _dbContext;

        public ProductCreatedHandler(ProductCatalogDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        [TransactionScope]
        public async Task<CreateProductResponse> Handle(CreateProductRequest request,
            CancellationToken cancellationToken)
        {
            var product = Product.Of(Guid.NewGuid(), request);
            var cats = await _dbContext.Categories.ToListAsync(cancellationToken: cancellationToken);
            var category = await _dbContext.Categories
                .FirstOrDefaultAsync(x => x.Id == request.CategoryId.ConvertTo<Guid>(),
                    cancellationToken: cancellationToken);

            if (category == null) throw new NullReferenceException("Couldn't find out {Category}");
            product.AssignCategory(category);

            var entityCreated = await _dbContext.Products.AddAsync(product, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            var productCreated = entityCreated.Entity;

            return new CreateProductResponse
            {
                Product = new CatalogProductDto
                {
                    Id = productCreated.Id.ToString(),
                    Name = productCreated.Name,
                    Description = productCreated.Description,
                    ImageUrl = productCreated.ImageUrl,
                    Price = productCreated.Price
                }
            };
        }
    }
}