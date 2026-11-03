# ADR 0001: BaseStorage keys are ignored by vanilla Valheim

## Status

Accepted

## Context

BaseStorage persists a small amount of data inside vanilla game data:

- `basestorage.cat`: the chest category (a category name, or Keep), in the chest's networked object data (ZDO).
- `basestorage.lock`: the organize lock record (holder id, holder name, expiry), in the ZDO of the lowest-id station in a base.
- `basestorage.fav`: the favourite inventory slots of versions before 0.4.1, in the player profile custom data. It is read once, converted and removed.
- `basestorage.favitems`: the favourite item types, in the player profile custom data.
- `basestorage.name`: a chest's name, stored on the chest and cleaned on every read.
- `basestorage.feed`: what a station is kept fed with and how much to leave in the base, in the station's ZDO.

Players must be able to remove the mod without damaging their world or characters.

## Decision

The mod stores nothing except values under these five keys, all prefixed `basestorage.`. Items always stay in vanilla inventories and containers; the mod never moves them into a private store. The keys are declared once, in `PersistenceKeys`, and each value is read and written only through its v1 codec (`CategoryCodec`, `OrganizeLockCodec`, `FavouritesCodec`, `FavouriteItems`, `FeedSelection`).

Vanilla Valheim ignores ZDO keys and profile custom-data keys it does not know, so these values are inert without the mod.

A malformed, truncated or foreign value decodes to no value (null) and never throws, so a hand-edited or old save cannot break loading.

## Consequences

- Uninstalling the mod leaves every chest and item intact. A leftover key changes no vanilla behaviour.
- Vanilla does not remove the leftover keys, so they stay in the world save and the profile after uninstall.
- Reinstalling the mod reads the leftover keys again through the v1 codecs.
- Adding a new key later is additive but must also be added to this ADR's key list.
- Changing a wire format needs a new format tag and a migration story; v1 decoders return no value for unknown tags.
