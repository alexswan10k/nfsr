#load "Store.fsx"

open System

type Cache<'a> =
    {
        Item: 'a; 
        Expiry: DateTime
    }

type CacheFileStore<'a>(expiry: TimeSpan, store: Store.FileStore<Cache<'a>>) =
    //let store = Store.FileStore<Cache<'a>>(path)
    let buildCache (buildItem : unit -> 'a) () = 
        {
            Item = buildItem();
            Expiry = DateTime.Now + expiry
        }

    member x.GetOrCreate (buildItem : unit -> 'a) =
        let buildCacheAndItem = buildCache buildItem
        let cval = store.GetOrCreate buildCacheAndItem
        if cval.Expiry < DateTime.Now then
            let newVal = buildCacheAndItem()
            store.Set newVal
            newVal
        else
            cval

    member x.Overwrite (buildItem : unit -> 'a) =
        let buildCacheAndItem = buildCache buildItem
        let newVal = buildCacheAndItem()
        store.Set newVal
        newVal