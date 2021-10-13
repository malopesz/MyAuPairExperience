using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Repository.Interface;

namespace Repository
{
    public abstract class BaseRepository : IBaseRepository
    {
        protected BaseRepository()
        {      
        }

        public virtual DbContext GetContext()
        {
            throw new NotImplementedException();
        }

        public async Task<List<T>> Get<T>(Expression<Func<T, bool>> predicate)
            where T : class
        {
            var result = new List<T>();
            try
            {
                if (predicate != null)
                {
                    result = await GetContext().Set<T>().Where(predicate).ToListAsync();
                }
                else
                {
                    result = await GetContext().Set<T>().ToListAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }

        public async Task<List<T>> Get<T>(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
            where T : class
        {
            using (var Context = GetContext())
            {
                var query = Context.Set<T>().Where(predicate);
                var aggregateQuery = includes.Aggregate(query, (current, include) => current.Include(include));
                return await aggregateQuery.ToListAsync();
            }
        }

        public virtual async Task<int> Count<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            try
            {
                using (var context = GetContext())
                {
                    var hasAnyRecordsInDatabase = context.Set<TEntity>().AsNoTracking().Any(predicate);

                    return predicate != null && hasAnyRecordsInDatabase
                        ? await context.Set<TEntity>().AsNoTracking().CountAsync(predicate)
                        : 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
        }

        public virtual async Task<TEntity> FirstOrDefault<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            try
            {
                return await (GetContext().Set<TEntity>()).FirstOrDefaultAsync(predicate);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public async Task<T> Add<T>(T item)
            where T : class
        {
            var context = GetContext();
            var result = await context.Set<T>().AddAsync(item);
            await context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<List<T>> Add<T>(List<T> items)
            where T : class
        {
            using (var Context = GetContext())
            {
                await Context.Set<T>().AddRangeAsync(items);
                await Context.SaveChangesAsync();
                return items;
            }
        }

        public async Task<T> Update<T>(T item, Expression<Func<T, bool>> idPredicate) where T : class
        {
            using (var Context = GetContext())
            {
                var updateItem = Context.Entry(await Context.Set<T>().FirstOrDefaultAsync(idPredicate));
                updateItem.State = EntityState.Modified;
                updateItem.CurrentValues.SetValues(item);
                await Context.SaveChangesAsync();
                return updateItem.Entity;
            }
        }

        public async Task<List<T>> Update<T>(List<T> items, List<string> updateByProperties) where T : class
        {
            using (var Context = GetContext())
            {
                await Context.BulkUpdateAsync(items, config => { config.UpdateByProperties = updateByProperties; });

                return items;
            }
        }

        public async Task<bool> Delete<T>(Expression<Func<T, bool>> deletePredicate, bool deleteMany = false) where T : class
        {
            using (var Context = GetContext())
            {
                var deleteItems = await Context.Set<T>().Where(deletePredicate).ToListAsync();

                if (deleteMany)
                {
                    Context.RemoveRange(deleteItems);
                }
                else
                {
                    var deleteItem = deleteItems.FirstOrDefault();

                    if (deleteItem == null)
                    {
                        return false;
                    }

                    Context.Remove(deleteItem);
                }

                await Context.SaveChangesAsync();

                return true;
            }
        }

        public async Task Delete<TEntity>(TEntity record) where TEntity : class
        {
            try
            {
                using (var context = GetContext())
                {
                    context.Set<TEntity>().Attach(record);
                    context.Set<TEntity>().Remove(record);
                    await context.SaveChangesAsync();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task<TEntity> Insert<TEntity>(TEntity record) where TEntity : class
        {
            try
            {
                using (var context = GetContext())
                {
                    var result = context.Set<TEntity>().Add(record);
                    await context.SaveChangesAsync();

                    return result.Entity;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
    }
}