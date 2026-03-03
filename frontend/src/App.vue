<script setup lang="ts">
import { onLaunch, onShow, onHide } from '@dcloudio/uni-app'

onLaunch(() => {
  console.log('App Launch')
})

onShow(() => {
  console.log('App Show')
})

onHide(() => {
  console.log('App Hide')
})
</script>

<style>
/* 全局样式重置 */
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

page {
  background-color: #16213e;
  color: #ffffff;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
  width: 100%;
  max-width: 750rpx;
  margin: 0 auto;
}

/* 页面容器居中 */
.container {
  width: 100%;
  max-width: 750rpx;
  margin: 0 auto;
  padding: 40rpx;
  box-sizing: border-box;
}
</style>
