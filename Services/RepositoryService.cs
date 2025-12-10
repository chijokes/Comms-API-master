using FusionComms.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FusionComms.Services
{
    public interface IRepository
    {
        /// <summary>
        /// Gets the underlying http context
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// Gets the underlying automapper instance
        /// </summary>
        //public IMapper Mapper { get; }

        /// <summary>
        /// Gets the underlying database context.
        /// </summary>
        //public ApplicationDbContext DbContext { get; }

        ValueTask<bool> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
        ValueTask<bool> AddRangeAsync<T>(List<T> entity, CancellationToken cancellationToken = default) where T : class;
        IQueryable<T> ListAll<T>() where T : class;
        ValueTask<T> FindAsync<T>(string Id) where T : class;
        ValueTask<bool> AnyAsync<T>(string Id) where T : class;

        /// <summary>
        /// Enables or disables a database record.
        /// </summary>
        ValueTask<bool> ModifyAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
        ValueTask<bool> DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
    }

    public class Repository : IRepository
    {
        public HttpContext HttpContext { get; }

        //public IMapper Mapper { get; }

        private AppDbContext DbContext { get; }

        public Repository(AppDbContext dbContext, IHttpContextAccessor contextAccessor)
        {
            DbContext = dbContext;
            HttpContext = contextAccessor.HttpContext;
        }



        public IQueryable<T> ListAll<T>() where T : class
        {
            return DbContext.Set<T>();
        }

        public async ValueTask<T> FindAsync<T>(string Id) where T : class
        {
            if (string.IsNullOrWhiteSpace(Id))
                return null;

            return await DbContext.FindAsync<T>(Id);
        }


        public async ValueTask<bool> AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
        {
            await DbContext.AddAsync(entity, cancellationToken);
            return await DbContext.SaveChangesAsync(cancellationToken) > 0;
        }


        public async ValueTask<bool> AddRangeAsync<T>(List<T> entity, CancellationToken cancellationToken = default) where T : class
        {
            await DbContext.AddRangeAsync(entity, cancellationToken);
            return await DbContext.SaveChangesAsync(cancellationToken) > 0;
        }

        public async ValueTask<bool> ModifyAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
        {
            DbContext.Update<T>(entity);
            return await DbContext.SaveChangesAsync(cancellationToken) > 0;
        }

        public async ValueTask<bool> DeleteAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
        {
            DbContext.Remove<T>(entity);
            return await DbContext.SaveChangesAsync(cancellationToken) > 0;
        }

        public async ValueTask<bool> AnyAsync<T>(string Id) where T : class
        {
            var result = await ListAll<T>().AnyAsync<T>();
            return result;
        }
    }
}
