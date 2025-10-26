create table "work_items" (
  "id" INT not null,
  "title" varchar(255) not null,
  "tag" varchar(255) not null,
  "state" varchar(255) not null,
  constraint "work_items_pkey" primary key ("id")
);