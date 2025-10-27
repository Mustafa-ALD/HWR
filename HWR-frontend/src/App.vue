<script setup>
import { onMounted, ref } from 'vue'
import axios from 'axios'

const bugs = ref(0)
const features = ref(0)

onMounted(async () => {
  let url = 'http://localhost:5169/work-items'
  let headers = {
    Accept: 'application/json',
    'Content-Type': 'application/json;charset=UTF-8',
  }
  let response = await axios.get(url, headers)
  let responseOK = response && response.status === 200 && response.statusText === 'OK'
  
  if (responseOK) {
    let data = await response.data
    for(const entry of data) {
      if (entry.tag === "Feature") {
        features.value++
      } else if (entry.tag === "Bug") {
        bugs.value++
      }
    }
  }
})
</script>

<template>
  <header>
    <div class="outer">
      <h1>Dashboard</h1>
      <div class="counters-box">
        <div class="box">
          <span class="box-title">Bugs</span>
          <span class="box-number">{{ bugs }}</span>
        </div>
        <div class="box">
          <span class="box-title">Features</span>
          <span class="box-number">{{ features }}</span>
        </div>
      </div>
    </div>
  </header>
</template>

<style scoped>
.box-title {
  font-size: medium;
  font-weight: 500;
}
.box-number {
  font-weight: bold;
  font-size: larger;
}
.box {
  border-radius: 20px;
  border: 1px solid darkgrey;
  padding: 5px 20px;
  display: flex;
  flex-direction: column;
  text-align: center;
}
.counters-box{
  display: flex;
  width: 100%;
  align-items: center;
  justify-content: center;
  gap: 4rem;
}

.outer{
 display: flex;
 align-items: center;
 justify-content: center;
 flex-direction: column;
 gap: 8px;
}

header {
  line-height: 1.5;
  max-height: 100vh;
}

.logo {
  display: block;
  margin: 0 auto 2rem;
}

nav {
  width: 100%;
  font-size: 12px;
  text-align: center;
  margin-top: 2rem;
}

nav a.router-link-exact-active {
  color: var(--color-text);
}

nav a.router-link-exact-active:hover {
  background-color: transparent;
}

nav a {
  display: inline-block;
  padding: 0 1rem;
  border-left: 1px solid var(--color-border);
}

nav a:first-of-type {
  border: 0;
}

@media (min-width: 1024px) {
  header {
    display: flex;
    place-items: center;
    padding-right: calc(var(--section-gap) / 2);
  }

  .logo {
    margin: 0 2rem 0 0;
  }

  header .wrapper {
    display: flex;
    place-items: flex-start;
    flex-wrap: wrap;
  }

  nav {
    text-align: left;
    margin-left: -1rem;
    font-size: 1rem;

    padding: 1rem 0;
    margin-top: 1rem;
  }
}
</style>
