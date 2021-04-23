<template>
	<v-app>
		<Header/>
		<v-main>
			<v-container>
				<router-view/>
			</v-container>
		</v-main>
		<v-footer id="ws-footer" app>
			<router-link to="/">Home</router-link> |
			<router-link to="/about">About</router-link>
		</v-footer>
	</v-app>
</template>

<script lang="ts">
import { Component, Vue } from "vue-property-decorator"
import Header from "@/components/Header.vue"

@Component({
	components: {
		Header
	}
})
export default class App extends Vue {}
</script>