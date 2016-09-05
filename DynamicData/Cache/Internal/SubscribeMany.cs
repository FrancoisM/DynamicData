﻿using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Internal;

namespace DynamicData.Cache.Internal
{
    internal class SubscribeMany<TObject, TKey>
    {
        private readonly IObservable<IChangeSet<TObject, TKey>> _source;
        private readonly Func<TObject,TKey, IDisposable> _subscriptionFactory;

        public SubscribeMany(IObservable<IChangeSet<TObject, TKey>> source,  Func<TObject, IDisposable> subscriptionFactory)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (subscriptionFactory == null) throw new ArgumentNullException(nameof(subscriptionFactory));

            _source = source;
            _subscriptionFactory = (t, key) => subscriptionFactory(t);
        }

        public SubscribeMany(IObservable<IChangeSet<TObject, TKey>> source, Func<TObject, TKey, IDisposable> subscriptionFactory)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (subscriptionFactory == null) throw new ArgumentNullException(nameof(subscriptionFactory));

            _source = source;
            _subscriptionFactory = subscriptionFactory;
        }

        public IObservable<IChangeSet<TObject, TKey>> Run()
        {

            return Observable.Create<IChangeSet<TObject, TKey>>
                (
                    observer =>
                    {
                        var published = _source.Publish();
                        var subscriptions = published
                            .Transform((t, k) => new SubscriptionContainer<TObject, TKey>(t, k, _subscriptionFactory))
                            .DisposeMany()
                            .Subscribe();

                        var result = published.SubscribeSafe(observer);
                        var connected = published.Connect();

                        return new CompositeDisposable(subscriptions, connected, result);
                    });
        }
    }
}